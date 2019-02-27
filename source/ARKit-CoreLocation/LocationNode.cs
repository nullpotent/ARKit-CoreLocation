using CoreLocation;
using SceneKit;

namespace ARCL
{
    /// <summary>
    /// A location node can be added to a scene using a coordinate.
    /// Its scale and position should not be adjusted, as these are used for scene layout purposes
    /// To adjust the scale and position of items within a node, you can add them to a child node and adjust them there
    /// </summary>
    public class LocationNode : SCNNode
    {
        /// <summary>
        /// Gets or sets the location.
        /// Location can be changed and confirmed later by SceneLocationView.
        /// </summary>
        /// <value>The location.</value>
        public CLLocation Location { get; set; }

        /// <summary>
        /// Gets or sets the tag.
        /// A general purpose tag that can be used to find nodes already added to a SceneLocationView
        /// </summary>
        /// <value>The tag.</value>
        public string Tag { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:ARCL_iOS.LocationNode"/> location confirmed.
        /// This is automatically set to true when you create a node using a location.
        /// Otherwise, this is false, and becomes true once the user moves 100m away from the node,
        /// except when the locationEstimateMethod is set to use Core Location data only,
        /// as then it becomes true immediately.
        /// </summary>
        /// <value><c>true</c> if node location is confirmed; otherwise, <c>false</c>.</value>
        public bool LocationConfirmed { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether a node's position and scale should be updated automatically on a continual basis.
        /// This should only be set to false if you plan to manually update position and scale
        /// at regular intervals. You can do this with `SceneLocationView`'s `updatePositionOfLocationNode`.
        /// </summary>
        /// <value><c>true</c> if node should continually adjust position when within range; otherwise, <c>false</c>.</value>
        public bool ContinuallyAdjustNodePositionWhenWithinRange { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:ARCL_iOS.LocationNode"/>
        /// node's position and scale should be updated automatically on a continual basis.
        /// This should only be set to false if you plan to manually update position and scale
        /// at regular intervals. You can do this with `SceneLocationView`'s `updatePositionOfLocationNode`.
        /// </summary>
        /// <value><c>true</c> if continually update position and scale; otherwise, <c>false</c>.</value>
        public bool ContinuallyUpdatePositionAndScale { get; set; } = true;

        public LocationNode(CLLocation location)
        {
            Location = location;
            LocationConfirmed = location != null;
        }
    }
}
