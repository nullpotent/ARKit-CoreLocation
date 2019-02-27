using CoreGraphics;
using SceneKit;
using UIKit;

namespace ARCL.Extensions
{
    public static class SCNNodeFactory
    {
        public static SCNNode AxesNode(float quiverLength, float quiverThickness)
        {
            quiverThickness = (quiverLength / 50f) * quiverThickness;
            var chamferRadius = quiverThickness / 2f;

            var xQuiverBox = new SCNBox
            {
                Width = quiverLength,
                Height = quiverThickness,
                Length = quiverThickness,
                ChamferRadius = chamferRadius
            };
            if (xQuiverBox.FirstMaterial != null)
                xQuiverBox.FirstMaterial.Diffuse.Contents = UIColor.Red;

            var xQuiverNode = new SCNNode
            {
                Geometry = xQuiverBox,
                Position = new SCNVector3(quiverLength / 2f, 0f, 0f)
            };

            var yQuiverBox = new SCNBox
            {
                Width = quiverThickness,
                Height = quiverLength,
                Length = quiverThickness,
                ChamferRadius = chamferRadius
            };
            if (yQuiverBox.FirstMaterial != null)
                yQuiverBox.FirstMaterial.Diffuse.Contents = UIColor.Green;

            var yQuiverNode = new SCNNode
            {
                Geometry = yQuiverBox,
                Position = new SCNVector3(0f, quiverLength / 2f, 0f)
            };

            var zQuiverBox = new SCNBox
            {
                Width = quiverThickness,
                Height = quiverThickness,
                Length = quiverLength,
                ChamferRadius = chamferRadius
            };
            if (zQuiverBox.FirstMaterial != null)
                zQuiverBox.FirstMaterial.Diffuse.Contents = UIColor.Blue;

            var zQuiverNode = new SCNNode
            {
                Geometry = zQuiverBox,
                Position = new SCNVector3(0f, 0f, quiverLength / 2f)
            };

            var quiverNode = new SCNNode();
            quiverNode.AddChildNode(xQuiverNode);
            quiverNode.AddChildNode(yQuiverNode);
            quiverNode.AddChildNode(zQuiverNode);
            quiverNode.Name = "Axes";
            return quiverNode;
        }
    }

    public static class SCNNode_Extensions
    {
        public static void Center(this SCNNode self)
        {
            SCNVector3 min = SCNVector3.Zero, max = SCNVector3.Zero;
            self.GetBoundingBox(ref min, ref max);
            self.Pivot = SCNMatrix4.CreateTranslation(min.X + (self.Width() * 0.5F), min.Y + (self.Height() * 0.5F), 0);
        }

        public static void AlignTopTo(this SCNNode self, SCNNode from, float offset = 0)
        {
            var y = self.ConvertPositionFromNode(new SCNVector3(0, from.Height() * 0.5F, 0), from).Y - (self.Height() * 0.5F) + offset;
            self.Position = new SCNVector3(self.Position.X, y, self.Position.Z);
        }

        public static void AlignBottomTo(this SCNNode self, SCNNode from, float offset = 0)
        {
            var y = self.ConvertPositionFromNode(new SCNVector3(0, -from.Height() * 0.5F, 0), from).Y + (self.Height() * 0.5F) + offset;
            self.Position = new SCNVector3(self.Position.X, y, self.Position.Z);
        }

        public static void AlignLeftTo(this SCNNode self, SCNNode from, float offset = 0)
        {
            var x = self.ConvertPositionFromNode(new SCNVector3(-from.Width() * 0.5F, 0, 0), from).X + (self.Width() * 0.5F) + offset;
            self.Position = new SCNVector3(x, self.Position.Y, self.Position.Z);
        }

        public static void AlignRightTo(this SCNNode self, SCNNode from, float offset = 0)
        {
            var x = self.ConvertPositionFromNode(new SCNVector3(from.Width() * 0.5F, 0, 0), from).X - (self.Width() * 0.5F) + offset;
            self.Position = new SCNVector3(x, self.Position.Y, self.Position.Z);
        }

        public static void AlignCenterHorizontallyWith(this SCNNode self, SCNNode from, float offset = 0)
        {
            var conv = self.ConvertPositionFromNode(SCNVector3.Zero, from);
            self.Position = new SCNVector3(conv.X + offset, self.Position.Y, self.Position.Z);
        }

        public static void AlignCenterVerticallyWith(this SCNNode self, SCNNode from, float offset = 0)
        {
            var conv = self.ConvertPositionFromNode(SCNVector3.Zero, from);
            self.Position = new SCNVector3(self.Position.X, conv.Y + offset, self.Position.Z);
        }

        public static float Width(this SCNNode self)
        {
            return (float)self.Size().Width;
        }

        public static float Height(this SCNNode self)
        {
            return (float)self.Size().Height;
        }

        public static CGSize Size(this SCNNode self)
        {
            SCNVector3 min = SCNVector3.Zero, max = SCNVector3.Zero;
            self.GetBoundingBox(ref min, ref max);
            return new CGSize(max.X - min.X, max.Y - min.Y);
        }
    }
}
