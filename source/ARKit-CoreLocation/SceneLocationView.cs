using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ARCL.Extensions;
using ARKit;
using CoreGraphics;
using CoreLocation;
using Foundation;
using ObjCRuntime;
using SceneKit;

namespace ARCL
{
    public class SceneLocationView : ARSCNView, IARSCNViewDelegate, ILocationManagerDelegate
    {
        public ISceneLocationViewDelegate LocationDelegate
        {
            get => weakLocationDelegate.TryGetTarget(out var @delegate) ? @delegate : null;
            set => weakLocationDelegate?.SetTarget(value);
        }

        public LocationEstimateMethod LocationEstimateMethod { get; set; } = LocationEstimateMethod.MostRelevantEstimate;

        public LocationManager LocationManager { get; private set; } = new LocationManager();

        public bool ShowAxesNode { get; set; }

        public IList<LocationNode> LocationNodes { get; } = new List<LocationNode>();

        public SCNNode SceneNode
        {
            get => sceneNode;
            set
            {
                sceneNode = value;
                if (sceneNode != null)
                {
                    foreach (var locationNode in LocationNodes)
                    {
                        sceneNode.AddChildNode(locationNode);
                    }

                    LocationDelegate?.SceneLocationViewDidSetupSceneNode(this, sceneNode);
                }
            }
        }

        public bool ShowFeaturePoints { get; set; } = false;

        public bool OrientToTrueNorth { get; set; } = true;

        static readonly double sceneLimit = 100.0;
        readonly WeakReference<ISceneLocationViewDelegate> weakLocationDelegate = new WeakReference<ISceneLocationViewDelegate>(null);

        List<SceneLocationEstimate> sceneLocationEstimates = new List<SceneLocationEstimate>();
        NSTimer updateEstimatesTimer;
        bool didFetchInitialLocation = false;
        SCNNode sceneNode;

        public SceneLocationView()
        : this(CGRect.Empty)
        {
        }

        public SceneLocationView(CGRect frame)
        {
            Frame = frame;
            FinishInitialization();
        }

        public SceneLocationView(NSCoder decoder)
        : base(decoder)
        {
            FinishInitialization();
        }

        void FinishInitialization()
        {
            LocationManager.Delegate = this;
            Delegate = this;

            // Show statistics such as fps and timing information
            ShowsStatistics = false;

            if (ShowFeaturePoints)
            {
                DebugOptions = ARSCNDebugOptions.ShowFeaturePoints;
            }
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();
        }

        public void Run()
        {
            var configuration = new ARWorldTrackingConfiguration
            {
                PlaneDetection = ARPlaneDetection.Horizontal,
                WorldAlignment = OrientToTrueNorth ? ARWorldAlignment.GravityAndHeading : ARWorldAlignment.Gravity
            };

            Session.Run(configuration, ARSessionRunOptions.None);

            updateEstimatesTimer?.Invalidate();
            updateEstimatesTimer = NSTimer.CreateScheduledTimer(0.1, this, new Selector(nameof(UpdateLocationData)), null, true);
        }

        public void Pause()
        {
            Session.Pause();
            updateEstimatesTimer?.Invalidate();
            updateEstimatesTimer = null;
        }

        [Export(nameof(UpdateLocationData))]
        void UpdateLocationData()
        {
            RemoveOldLocationEstimates();
            ConfirmLocationOfDistantLocationNodes();
            UpdatePositionAndScaleOfLocationNodes();
        }

        public void MoveSceneHeadingClockwise()
        {
            if (SceneNode == null)
            {
                return;
            }

            SceneNode.EulerAngles = new SCNVector3(SceneNode.EulerAngles.X, SceneNode.EulerAngles.Y - 1f.DegreesToRadians(), SceneNode.EulerAngles.Z);
        }

        public void MoveSceneHeadingAntiClockwise()
        {
            if (SceneNode == null)
            {
                return;
            }

            SceneNode.EulerAngles = new SCNVector3(SceneNode.EulerAngles.X, SceneNode.EulerAngles.Y + 1f.DegreesToRadians(), SceneNode.EulerAngles.Z);
        }

        void ResetSceneHeading()
        {
            if (SceneNode == null)
            {
                return;
            }

            SceneNode.EulerAngles = new SCNVector3(SceneNode.EulerAngles.X, 0, SceneNode.EulerAngles.Z);
        }

        public SCNVector3? CurrentScenePosition()
        {
            if (!(PointOfView is SCNNode pointOfView))
            {
                return null;
            }

            return Scene.RootNode.ConvertPositionToNode(pointOfView.Position, SceneNode);
        }

        public SCNVector3? CurrentEulerAngles()
        {
            return PointOfView?.EulerAngles;
        }

        protected void AddSceneLocationEstimate(CLLocation location)
        {
            if (!(CurrentScenePosition() is SCNVector3 position))
            {
                return;
            }

            var sceneLocationEstimate = new SceneLocationEstimate(location, position);
            sceneLocationEstimates.Add(sceneLocationEstimate);
            if (sceneLocationEstimates.Count > 30)
            {
                sceneLocationEstimates.RemoveAt(0);
            }

            LocationDelegate?.SceneLocationViewDidAddSceneLocationEstimate(this, position, location);
        }

        void RemoveOldLocationEstimates()
        {
            if (!(CurrentScenePosition() is SCNVector3 position))
            {
                return;
            }

            RemoveOldLocationEstimates(position);
        }

        void RemoveOldLocationEstimates(SCNVector3 currentScenePosition)
        {
            var currentPoint = CGPointFactory.PointWithVector(currentScenePosition);
            sceneLocationEstimates = sceneLocationEstimates.Where(@estimate =>
            {
                var point = CGPointFactory.PointWithVector(@estimate.Position);
                var radiusContainsPoint = currentPoint.RadiusContainsPoint(sceneLimit, point);
                if (!radiusContainsPoint)
                {
                    LocationDelegate?.SceneLocationViewDidRemoveSceneLocationEstimate(this, @estimate.Position, @estimate.Location);
                }
                return radiusContainsPoint;
            }).ToList();
        }

        public SceneLocationEstimate BestLocationEstimate()
        {
            var sortedLocationEstimates = sceneLocationEstimates.ToList();
            sortedLocationEstimates.Sort((a, b) =>
            {
                if (Math.Abs(a.Location.HorizontalAccuracy - b.Location.HorizontalAccuracy) < double.Epsilon)
                {
                    // TODO: double check
                    return (int)b.Location.Timestamp.Compare(a.Location.Timestamp);
                }
                return a.Location.HorizontalAccuracy.CompareTo(b.Location.HorizontalAccuracy);
            });
            return sortedLocationEstimates?.FirstOrDefault();
        }

        public CLLocation CurrentLocation()
        {
            if (LocationEstimateMethod == LocationEstimateMethod.CoreLocationDataOnly)
            {
                return LocationManager.CurrentLocation;
            }

            if (!(BestLocationEstimate() is SceneLocationEstimate estimate &&
                  CurrentScenePosition() is SCNVector3 position))
            {
                return null;
            }

            return estimate.TranslatedLocation(position);
        }

        public void AddLocationNodeForCurrentPosition(LocationNode locationNode)
        {
            if (!(CurrentScenePosition() is SCNVector3 currentPosition &&
                  CurrentLocation() is CLLocation currentLocation &&
                  SceneNode is SCNNode sceneNode))
            {
                return;
            }

            locationNode.Location = currentLocation;
            locationNode.LocationConfirmed = LocationEstimateMethod == LocationEstimateMethod.CoreLocationDataOnly;
            locationNode.Position = currentPosition;
            LocationNodes.Add(locationNode);
            sceneNode.AddChildNode(locationNode);
        }

        public void AddLocationNodeWithConfirmedLocation(LocationNode locationNode)
        {
            if (locationNode.Location == null || locationNode.LocationConfirmed == false)
            {
                return;
            }

            UpdatePositionAndScaleOfLocationNode(locationNode, initialSetup: true, animated: false);
            LocationNodes.Add(locationNode);
            SceneNode?.AddChildNode(locationNode);
        }

        public void RemoveAllNodes()
        {
            LocationNodes.Clear();
            if (!(SceneNode?.ChildNodes is SCNNode[] childNodes))
            {
                return;
            }

            foreach (var node in childNodes)
            {
                node.RemoveFromParentNode();
            }
        }

        public bool SceneContainsNodeWithTag(string tag)
        {
            return FindNodes(tagged: tag).Any();
        }

        public IEnumerable<LocationNode> FindNodes(string tagged)
        {
            return LocationNodes.Where(node => node.Tag == tagged);
        }

        public void RemoveLocationNode(LocationNode locationNode)
        {
            var index = LocationNodes.IndexOf(locationNode);
            if (index >= 0)
            {
                LocationNodes.RemoveAt(index);
            }
            locationNode.RemoveFromParentNode();
        }

        void ConfirmLocationOfDistantLocationNodes()
        {
            if (!(CurrentScenePosition() is SCNVector3 currentPosition))
            {
                return;
            }

            foreach (var locationNode in LocationNodes.Where(node => !node.LocationConfirmed))
            {
                var currentPoint = CGPointFactory.PointWithVector(vector: currentPosition);
                var locationNodePoint = CGPointFactory.PointWithVector(vector: locationNode.Position);
                if (!currentPoint.RadiusContainsPoint(radius: SceneLocationView.sceneLimit, point: locationNodePoint))
                {
                    ConfirmLocationOfLocationNode(locationNode);
                }
            }
        }

        internal CLLocation LocationOfLocationNode(LocationNode locationNode)
        {
            if (locationNode.LocationConfirmed || LocationEstimateMethod == LocationEstimateMethod.CoreLocationDataOnly)
            {
                return locationNode.Location;
            }

            if ((BestLocationEstimate() is SceneLocationEstimate bestLocationEstimate) &&
                (locationNode.Location == null || bestLocationEstimate.Location.HorizontalAccuracy < locationNode.Location.HorizontalAccuracy))
            {
                var translatedLocation = bestLocationEstimate.TranslatedLocation(to: locationNode.Position);
                return translatedLocation;
            }
            else
            {
                return locationNode.Location;
            }
        }

        void ConfirmLocationOfLocationNode(LocationNode locationNode)
        {
            locationNode.Location = LocationOfLocationNode(locationNode);
            locationNode.LocationConfirmed = true;
            LocationDelegate?.SceneLocationViewDidConfirmLocationOfNode(sceneLocationView: this, node: locationNode);
        }

        void UpdatePositionAndScaleOfLocationNodes()
        {
            foreach (var locationNode in LocationNodes.Where(node => node.ContinuallyUpdatePositionAndScale))
            {
                UpdatePositionAndScaleOfLocationNode(locationNode: locationNode, animated: true);
            }
        }

        void UpdatePositionAndScaleOfLocationNode(LocationNode locationNode, bool initialSetup = false, bool animated = false, double duration = 0.1)
        {
            if (!(CurrentScenePosition() is SCNVector3 currentPosition && CurrentLocation() is CLLocation currentLocation))
            {
                return;
            }

            SCNTransaction.Begin();
            SCNTransaction.AnimationDuration = animated ? duration : 0;

            var locationNodeLocation = LocationOfLocationNode(locationNode);

            // Position is set to a position coordinated via the current position
            var locationTranslation = currentLocation.Translation(toLocation: locationNodeLocation);
            var adjustedDistance = default(double);
            var distance = locationNodeLocation.DistanceFrom(currentLocation);

            if (locationNode.LocationConfirmed &&
               (distance > 100 || locationNode.ContinuallyAdjustNodePositionWhenWithinRange || initialSetup))
            {
                if (distance > 100)
                {
                    // If the item is too far away, bring it closer and scale it down
                    var scale = 100 / distance;
                    adjustedDistance = distance * scale;

                    var adjustedTranslation = new SCNVector3(
                        x: (float)(locationTranslation.LongitudeTranslation * scale),
                        y: (float)(locationTranslation.AltitudeTranslation * scale),
                        z: (float)(locationTranslation.LatitudeTranslation * scale));

                    var position = new SCNVector3(
                        x: currentPosition.X + adjustedTranslation.X,
                        y: currentPosition.Y + adjustedTranslation.Y,
                        z: currentPosition.Z - adjustedTranslation.Z);

                    locationNode.Position = position;

                    locationNode.Scale = new SCNVector3(x: (float)scale, y: (float)scale, z: (float)scale);
                }
                else
                {
                    adjustedDistance = distance;
                    var position = new SCNVector3(
                        x: (float)(currentPosition.X + locationTranslation.LongitudeTranslation),
                        y: (float)(currentPosition.Y + locationTranslation.AltitudeTranslation),
                        z: (float)(currentPosition.Z - locationTranslation.LatitudeTranslation));

                    locationNode.Position = position;
                    locationNode.Scale = new SCNVector3(x: 1, y: 1, z: 1);
                }
            }
            else
            {
                // Calculates distance based on the distance within the scene, as the location isn't yet confirmed
                adjustedDistance = currentPosition.Distance(to: locationNode.Position);
                locationNode.Scale = new SCNVector3(x: 1, y: 1, z: 1);
            }

            if (locationNode is LocationAnnotationNode annotationTextNode)
            {
                var distanceOrder = (int)(-distance * 100);

                // TODO: test this
                annotationTextNode.RenderingOrder = distanceOrder;
            }

            if (locationNode is LocationAnnotationNode annotationNode)
            {
                // The scale of a node with a billboard constraint applied is ignored
                // The annotation subnode itself, as a subnode, has the scale applied to it
                var appliedScale = locationNode.Scale;
                locationNode.Scale = new SCNVector3(x: 1, y: 1, z: 1);
                var scale = default(double);
                if (annotationNode.ScaleRelativeToDistance)
                {
                    scale = appliedScale.Y;
                    annotationNode.AnnotationNode.Scale = appliedScale;
                }
                else
                {
                    // Scale it to be an appropriate size so that it can be seen
                    scale = adjustedDistance * 0.181;

                    if (distance > 3000)
                    {
                        scale = (float)(scale * 0.75);
                    }

                    annotationNode.AnnotationNode.Scale = new SCNVector3(x: (float)scale, y: (float)scale, z: (float)scale);
                }
                annotationNode.Pivot = SCNMatrix4.CreateTranslation(0, (float)(-1.1 * scale), 0);
            }

            SCNTransaction.Commit();

            LocationDelegate?.SceneLocationViewDidUpdateLocationAndScaleOfLocationNode(sceneLocationView: this, locationNode: locationNode);
        }

        [Export("renderer:didRenderScene:atTime:")]
        public void DidRenderScene(ISCNSceneRenderer renderer, SCNScene scene, double timeInSeconds)
        {
            if (SceneNode == null)
            {
                SceneNode = new SCNNode();
            }

            scene.RootNode.AddChildNode(SceneNode);

            if (ShowAxesNode)
            {
                var axesNode = SCNNodeFactory.AxesNode(quiverLength: 0.1F, quiverThickness: 0.5F);
                SceneNode?.AddChildNode(axesNode);
            }

            if (!didFetchInitialLocation)
            {
                // Current frame and current location are required for this to be successful
                if (Session.CurrentFrame != null && LocationManager.CurrentLocation is CLLocation currentLocation)
                {
                    didFetchInitialLocation = true;
                    AddSceneLocationEstimate(location: currentLocation);
                }
            }
        }

        [Export("sessionWasInterrupted:")]
        public void WasInterrupted(ARSession session)
        {
            Debug.WriteLine("session was interrupted");
        }

        [Export("sessionInterruptionEnded:")]
        public void InterruptionEnded(ARSession session)
        {
            Debug.WriteLine("session interruption ended");
        }

        [Export("session:didFailWithError:")]
        public void DidFail(ARSession session, NSError error)
        {
            Debug.WriteLine($"session did fail with error: {error}");
        }

        [Export("session:cameraDidChangeTrackingState:")]
        public void CameraDidChangeTrackingState(ARSession session, ARCamera camera)
        {
            switch (camera.TrackingState)
            {
                case ARTrackingState.Limited when camera.TrackingStateReason == ARTrackingStateReason.InsufficientFeatures:
                    Debug.WriteLine("camera did change tracking state: limited, insufficient features");
                    break;
                case ARTrackingState.Limited when camera.TrackingStateReason == ARTrackingStateReason.ExcessiveMotion:
                    Debug.WriteLine("camera did change tracking state: limited, excessive motion");
                    break;
                case ARTrackingState.Limited when camera.TrackingStateReason == ARTrackingStateReason.Initializing:
                    Debug.WriteLine("camera did change tracking state: limited, initializing");
                    break;
                case ARTrackingState.Limited when camera.TrackingStateReason == ARTrackingStateReason.Relocalizing:
                    Debug.WriteLine("camera did change tracking state: limited, relocalizing");
                    break;
                case ARTrackingState.Normal:
                    Debug.WriteLine("camera did change tracking state: normal");
                    break;
                case ARTrackingState.NotAvailable:
                    Debug.WriteLine("camera did change tracking state: not available");
                    break;
            }
        }

        public void LocationManagerDidUpdateLocation(LocationManager locationManager, CLLocation location)
        {
            AddSceneLocationEstimate(location);
        }

        public void LocationManagerDidUpdateHeading(LocationManager locationManager, double heading, double accuracy)
        {
            // negative value means the heading will equal the `magneticHeading`, and we're interested in the `trueHeading`
            if (accuracy < 0)
            {
                return;
            }

            // heading of 0º means its pointing to the geographic North
            if (Math.Abs(heading) < double.Epsilon)
            {
                ResetSceneHeading();
            }
        }
    }
}
