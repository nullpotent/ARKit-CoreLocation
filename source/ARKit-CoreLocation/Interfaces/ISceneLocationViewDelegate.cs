using CoreLocation;
using SceneKit;

namespace ARCL
{
    public interface ISceneLocationViewDelegate
    {
        void SceneLocationViewDidAddSceneLocationEstimate(SceneLocationView sceneLocationView, SCNVector3 position, CLLocation location);

        void SceneLocationViewDidRemoveSceneLocationEstimate(SceneLocationView sceneLocationView, SCNVector3 position, CLLocation location);

        void SceneLocationViewDidConfirmLocationOfNode(SceneLocationView sceneLocationView, LocationNode node);

        void SceneLocationViewDidSetupSceneNode(SceneLocationView sceneLocationView, SCNNode sceneNode);

        void SceneLocationViewDidUpdateLocationAndScaleOfLocationNode(SceneLocationView sceneLocationView, LocationNode locationNode);
    }
}
