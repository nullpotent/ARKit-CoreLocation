using System;
using ARCL.Extensions;
using CoreGraphics;
using CoreLocation;
using Foundation;
using SceneKit;
using UIKit;

namespace ARCL
{
    public class LocationAnnotationNode : LocationNode
    {
        /// <summary>
        /// An image to use for the annotation
        /// When viewed from a distance, the annotation will be seen at the size provided
        /// e.g. if the size is 100x100px, the annotation will take up approx 100x100 points on screen.
        /// </summary>
        public UIImage Image { get; private set; }

        /// <summary>
        /// The annotation node.
        /// Subnodes and adjustments should be applied to this subnode
        /// Required to allow scaling at the same time as having a 2D 'billboard' appearance
        /// </summary>
        public SCNNode AnnotationNode { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:ARCL_iOS.LocationAnnotationNode"/> node should be scaled relative to its distance from the camera
        /// Default value (false) scales it to visually appear at the same size no matter the distance
        /// Setting to true causes annotation nodes to scale like a regular node
        /// Scaling relative to distance may be useful with local navigation-based uses
        /// For landmarks in the distance, the default is correct
        /// </summary>
        /// <value><c>true</c> if node should scale relative to distance; otherwise, <c>false</c>.</value>
        public bool ScaleRelativeToDistance { get; set; } = false;

        protected SCNPlane AnnotationPlane { get; private set; }

        public LocationAnnotationNode(CLLocation location, string image, string type, string name, string line1, string line2, double distance = 0)
        : base(location)
        {
            distance = 1;

            AnnotationNode = new SCNNode();
            AddChildNode(AnnotationNode);

            var bilboardConstraint = new SCNBillboardConstraint
            {
                FreeAxes = SCNBillboardAxis.Y
            };

            Constraints = new[] { bilboardConstraint };

            var content = CreateContentNode(name, line1, line2, new CGSize(4, 4), 8);
            var header = CreateHeaderNode(type, image, content.Size(), new CGSize(8, 12));
            var separator = CreateBackgroundPlaneNode(new CGSize(Math.Max(content.Width(), header.Width()), 1), UIColor.LightGray.ColorWithAlpha(0.5F), 0);
            var rootNode = CreateBackgroundPlaneNode(new CGSize(Math.Max(content.Width(), header.Width()), content.Height() + header.Height() + separator.Height()), UIColor.White, 0);

            AnnotationNode.AddChildNode(rootNode);
            rootNode.AddChildNode(header);
            header.AlignTopTo(rootNode, 0);
            header.AlignCenterHorizontallyWith(rootNode, 0);

            rootNode.AddChildNode(separator);
            rootNode.AddChildNode(content);
            separator.AlignTopTo(rootNode, -header.Height());
            content.AlignTopTo(rootNode, -(header.Height() + separator.Height()));
            content.AlignLeftTo(rootNode, 0);
            rootNode.Scale = new SCNVector3(rootNode.Scale.X * 0.01F, rootNode.Scale.Y * 0.01F, rootNode.Scale.Z * 0.01F);
            Opacity = 0.8F;
        }

        static SCNNode CreateTextNode(string text, UIFont font, UIColor color, UIColor bgColor, float kerning = 0)
        {
            kerning = 1;
            var attrString = new NSMutableAttributedString(text, font);
            var textGeometry = SCNText.Create(attrString, 0);
            textGeometry.FirstMaterial.Diffuse.Contents = UIColor.DarkTextColor;
            textGeometry.FirstMaterial.LightingModelName = SCNLightingModel.Constant;
            textGeometry.FirstMaterial.DoubleSided = true;
            textGeometry.FirstMaterial.ReadsFromDepthBuffer = false;
            textGeometry.FirstMaterial.WritesToDepthBuffer = false;
            textGeometry.ChamferRadius = 0f;
            var textNode = new SCNNode
            {
                Geometry = textGeometry
            };
            textNode.Center();
            var container = CreateBackgroundPlaneNode(textNode.Size(), bgColor, 0);
            container.AddChildNode(textNode);
            return container;
        }

        static SCNNode CreateBackgroundPlaneNode(CGSize size, UIColor color, float radius)
        {
            var planeGeometry = SCNPlane.Create(size.Width, size.Height);
            planeGeometry.CornerRadius = radius;
            planeGeometry.FirstMaterial.Diffuse.Contents = color;
            planeGeometry.FirstMaterial.LightingModelName = SCNLightingModel.Constant;
            planeGeometry.FirstMaterial.DoubleSided = true;
            planeGeometry.FirstMaterial.ReadsFromDepthBuffer = false;
            planeGeometry.FirstMaterial.WritesToDepthBuffer = false;
            var planeNode = new SCNNode
            {
                Geometry = planeGeometry
            };
            return planeNode;
        }

        static SCNNode CreateIconImageNode(string imageName, CGSize size)
        {
            var image = UIImage.FromBundle(imageName);
            var imageGeometry = SCNPlane.Create(size.Width, size.Height);
            imageGeometry.FirstMaterial.Diffuse.Contents = image;
            imageGeometry.FirstMaterial.LightingModelName = SCNLightingModel.Constant;
            imageGeometry.FirstMaterial.DoubleSided = true;
            imageGeometry.FirstMaterial.ReadsFromDepthBuffer = false;
            imageGeometry.FirstMaterial.WritesToDepthBuffer = false;
            var planeNode = new SCNNode
            {
                Geometry = imageGeometry
            };
            return planeNode;
        }

        SCNNode CreateContentNode(string name, string line1, string line2, CGSize margin, float verticalSpacing)
        {
            var nameText = CreateTextNode(name, UIFont.SystemFontOfSize(15, UIFontWeight.Regular), UIColor.DarkGray, UIColor.Clear);
            var line1Text = CreateTextNode(line1, UIFont.SystemFontOfSize(13, UIFontWeight.Thin), UIColor.DarkGray.ColorWithAlpha(0.9F), UIColor.Clear);
            var line2Text = CreateTextNode(line2, UIFont.SystemFontOfSize(12, UIFontWeight.Thin), UIColor.DarkGray.ColorWithAlpha(0.4F), UIColor.Clear);

            var container = CreateBackgroundPlaneNode(new CGSize(Math.Max(Math.Max(nameText.Width(), line1Text.Width()), line2Text.Width()) + (2 * margin.Width), nameText.Height() + line1Text.Height() + line2Text.Height() + (2 * margin.Height) + (2 * verticalSpacing)), UIColor.White, 0);

            container.AddChildNode(nameText);
            container.AddChildNode(line1Text);
            container.AddChildNode(line2Text);

            nameText.AlignLeftTo(container, (float)margin.Width);
            nameText.AlignTopTo(container, (float)-margin.Height);

            line1Text.AlignLeftTo(container, (float)margin.Width);
            line1Text.AlignTopTo(container, (float)-(nameText.Height() + margin.Height + verticalSpacing));

            line2Text.AlignLeftTo(container, (float)margin.Width);
            line2Text.AlignTopTo(container, (float)-(nameText.Height() + line1Text.Height() + margin.Height + (2 * verticalSpacing)));

            return container;
        }

        SCNNode CreateHeaderNode(string headerText, string imageName, CGSize contentSize, CGSize margin)
        {
            var title = CreateTextNode(headerText, UIFont.SystemFontOfSize(18, UIFontWeight.Medium), UIColor.DarkTextColor.ColorWithAlpha(0.65F), UIColor.Clear, 3);
            var titleIconSpacing = 6F;
            var icon = CreateIconImageNode(imageName, new CGSize(title.Height(), title.Height()));
            var size = new CGSize((nfloat)(Math.Max(contentSize.Width, title.Width() + icon.Width()) + (2 * margin.Width) + titleIconSpacing), title.Height() + margin.Height);
            var container = CreateBackgroundPlaneNode(size, UIColor.White, 0);
            container.AddChildNode(icon);
            container.AddChildNode(title);
            icon.AlignLeftTo(container, (float)margin.Width);
            title.AlignLeftTo(container, (float)(margin.Width + icon.Width() + titleIconSpacing));
            return container;
        }
    }
}
