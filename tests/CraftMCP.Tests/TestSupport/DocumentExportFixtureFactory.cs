using CraftMCP.Domain.Ids;
using CraftMCP.Domain.Models;
using CraftMCP.Domain.Nodes;
using CraftMCP.Domain.ValueObjects;

namespace CraftMCP.Tests.TestSupport;

internal static class DocumentExportFixtureFactory
{
    public static IEnumerable<object[]> FixtureCases()
    {
        yield return new object[] { "social-graphic.v1.json", CreateSocialGraphic() };
        yield return new object[] { "ui-mockup.v1.json", CreateUiMockup() };
        yield return new object[] { "slide.v1.json", CreateSlide() };
    }

    public static IEnumerable<object[]> ShuffledFixtureCases()
    {
        yield return new object[] { CreateSocialGraphic(), CreateSocialGraphic(shuffled: true) };
        yield return new object[] { CreateUiMockup(), CreateUiMockup(shuffled: true) };
        yield return new object[] { CreateSlide(), CreateSlide(shuffled: true) };
    }

    public static DocumentState CreateSocialGraphic(bool shuffled = false)
    {
        var backgroundId = NodeId.From("node_social_background");
        var heroGroupId = NodeId.From("node_social_hero_group");
        var imageId = NodeId.From("node_social_hero_image");
        var titleId = NodeId.From("node_social_title");
        var subtitleId = NodeId.From("node_social_subtitle");
        var ctaButtonId = NodeId.From("node_social_cta_button");
        var ctaLabelId = NodeId.From("node_social_cta_label");
        var heroAssetId = AssetId.From("asset_social_hero");

        var nodes = new List<KeyValuePair<NodeId, NodeBase>>
        {
            new(backgroundId, new RectangleNode(
                backgroundId,
                "Background",
                TransformValue.Identity,
                null,
                true,
                false,
                OpacityValue.Full,
                new SizeValue(1080, 1080),
                new ColorValue(245, 240, 232),
                null,
                0)),
            new(heroGroupId, new GroupNode(
                heroGroupId,
                "Hero Group",
                new TransformValue(120, 120, 1, 1, 0),
                null,
                true,
                false,
                OpacityValue.Full,
                new[] { imageId, titleId, subtitleId, ctaButtonId, ctaLabelId })),
            new(imageId, new ImageNode(
                imageId,
                "Hero Image",
                TransformValue.Identity,
                heroGroupId,
                true,
                false,
                OpacityValue.Full,
                new RectValue(0, 0, 360, 360),
                new AssetReference(heroAssetId),
                "cover",
                new RectValue(40, 20, 280, 280))),
            new(titleId, new TextNode(
                titleId,
                "Headline",
                TransformValue.Identity,
                heroGroupId,
                true,
                false,
                OpacityValue.Full,
                "Launch day, without the scramble.",
                new RectValue(400, 20, 440, 96),
                new TypographyStyle("Inter", 72, 700, "start", 1, 0),
                new ColorValue(34, 34, 34))),
            new(subtitleId, new TextNode(
                subtitleId,
                "Subheadline",
                TransformValue.Identity,
                heroGroupId,
                true,
                false,
                OpacityValue.Full,
                "Draft once, refine locally, export with full structure.",
                new RectValue(400, 144, 440, 112),
                new TypographyStyle("Inter", 28, 400, "start", 1.3, 0.2),
                new ColorValue(84, 84, 84))),
            new(ctaButtonId, new RectangleNode(
                ctaButtonId,
                "CTA Button",
                new TransformValue(400, 304, 1, 1, 0),
                heroGroupId,
                true,
                false,
                OpacityValue.Full,
                new SizeValue(240, 64),
                new ColorValue(34, 34, 34),
                null,
                12)),
            new(ctaLabelId, new TextNode(
                ctaLabelId,
                "CTA Label",
                new TransformValue(436, 322, 1, 1, 0),
                heroGroupId,
                true,
                false,
                OpacityValue.Full,
                "Export JSON",
                new RectValue(0, 0, 168, 28),
                new TypographyStyle("Inter", 24, 600, "center", 1, 0),
                new ColorValue(255, 255, 255))),
        };

        var assets = new List<KeyValuePair<AssetId, AssetManifestEntry>>
        {
            new(heroAssetId, new AssetManifestEntry(heroAssetId, "social-hero.png", "image/png", "hash-social-hero")),
        };

        return new DocumentState(
            DocumentId.From("doc_social_launch"),
            SchemaVersion.V1,
            "Social Launch",
            new CanvasModel(1080, 1080, CanvasPreset.SquarePost, new ColorValue(245, 240, 232)),
            ToDictionary(nodes, shuffled),
            new[] { backgroundId, heroGroupId },
            ToDictionary(assets, shuffled));
    }

    public static DocumentState CreateUiMockup(bool shuffled = false)
    {
        var frameId = NodeId.From("node_ui_phone_frame");
        var statusId = NodeId.From("node_ui_status");
        var titleId = NodeId.From("node_ui_title");
        var dividerId = NodeId.From("node_ui_divider");
        var cardGroupId = NodeId.From("node_ui_card_group");
        var avatarId = NodeId.From("node_ui_avatar");
        var nameId = NodeId.From("node_ui_name");
        var roleId = NodeId.From("node_ui_role");
        var buttonId = NodeId.From("node_ui_button");
        var buttonLabelId = NodeId.From("node_ui_button_label");
        var avatarAssetId = AssetId.From("asset_ui_avatar");

        var nodes = new List<KeyValuePair<NodeId, NodeBase>>
        {
            new(frameId, new RectangleNode(
                frameId,
                "Phone Frame",
                new TransformValue(523, 24, 1, 1, 0),
                null,
                true,
                false,
                OpacityValue.Full,
                new SizeValue(393, 852),
                new ColorValue(255, 255, 255),
                new StrokeStyle(new ColorValue(212, 220, 228), 2),
                36)),
            new(statusId, new TextNode(
                statusId,
                "Status",
                new TransformValue(560, 48, 1, 1, 0),
                null,
                true,
                false,
                OpacityValue.Full,
                "9:41",
                new RectValue(0, 0, 80, 24),
                new TypographyStyle("Inter", 20, 600, "start", 1, 0),
                new ColorValue(28, 32, 42))),
            new(titleId, new TextNode(
                titleId,
                "Profile Title",
                new TransformValue(580, 116, 1, 1, 0),
                null,
                true,
                false,
                OpacityValue.Full,
                "Profile overview",
                new RectValue(0, 0, 240, 36),
                new TypographyStyle("Inter", 30, 700, "start", 1, 0),
                new ColorValue(28, 32, 42))),
            new(dividerId, new LineNode(
                dividerId,
                "Divider",
                new TransformValue(580, 176, 1, 1, 0),
                null,
                true,
                false,
                OpacityValue.Full,
                new PointValue(0, 0),
                new PointValue(280, 0),
                new StrokeStyle(new ColorValue(220, 226, 232), 1))),
            new(cardGroupId, new GroupNode(
                cardGroupId,
                "Profile Card",
                new TransformValue(560, 220, 1, 1, 0),
                null,
                true,
                false,
                OpacityValue.Full,
                new[] { avatarId, nameId, roleId, buttonId, buttonLabelId })),
            new(avatarId, new ImageNode(
                avatarId,
                "Avatar",
                TransformValue.Identity,
                cardGroupId,
                true,
                false,
                OpacityValue.Full,
                new RectValue(0, 0, 120, 120),
                new AssetReference(avatarAssetId),
                "cover",
                null)),
            new(nameId, new TextNode(
                nameId,
                "Name",
                new TransformValue(152, 8, 1, 1, 0),
                cardGroupId,
                true,
                false,
                OpacityValue.Full,
                "Avery Stone",
                new RectValue(0, 0, 180, 30),
                new TypographyStyle("Inter", 26, 700, "start", 1, 0),
                new ColorValue(28, 32, 42))),
            new(roleId, new TextNode(
                roleId,
                "Role",
                new TransformValue(152, 48, 1, 1, 0),
                cardGroupId,
                true,
                false,
                OpacityValue.Full,
                "Lead product designer",
                new RectValue(0, 0, 220, 24),
                new TypographyStyle("Inter", 18, 400, "start", 1.2, 0),
                new ColorValue(106, 114, 125))),
            new(buttonId, new RectangleNode(
                buttonId,
                "Action Button",
                new TransformValue(152, 84, 1, 1, 0),
                cardGroupId,
                true,
                false,
                OpacityValue.Full,
                new SizeValue(160, 48),
                new ColorValue(28, 32, 42),
                null,
                14)),
            new(buttonLabelId, new TextNode(
                buttonLabelId,
                "Action Label",
                new TransformValue(192, 96, 1, 1, 0),
                cardGroupId,
                true,
                false,
                OpacityValue.Full,
                "Invite",
                new RectValue(0, 0, 80, 24),
                new TypographyStyle("Inter", 18, 600, "center", 1, 0),
                new ColorValue(255, 255, 255))),
        };

        var assets = new List<KeyValuePair<AssetId, AssetManifestEntry>>
        {
            new(avatarAssetId, new AssetManifestEntry(avatarAssetId, "avatar.png", "image/png", "hash-ui-avatar")),
        };

        return new DocumentState(
            DocumentId.From("doc_ui_profile"),
            SchemaVersion.V1,
            "UI Profile",
            new CanvasModel(1440, 900, CanvasPreset.DesktopFrame, new ColorValue(241, 245, 249)),
            ToDictionary(nodes, shuffled),
            new[] { frameId, statusId, titleId, dividerId, cardGroupId },
            ToDictionary(assets, shuffled));
    }

    public static DocumentState CreateSlide(bool shuffled = false)
    {
        var circleId = NodeId.From("node_slide_accent");
        var titleId = NodeId.From("node_slide_title");
        var bulletGroupId = NodeId.From("node_slide_bullets");
        var panelId = NodeId.From("node_slide_panel");
        var bulletOneId = NodeId.From("node_slide_bullet_one");
        var bulletTwoId = NodeId.From("node_slide_bullet_two");
        var bulletThreeId = NodeId.From("node_slide_bullet_three");
        var footerLineId = NodeId.From("node_slide_footer_line");

        var nodes = new List<KeyValuePair<NodeId, NodeBase>>
        {
            new(circleId, new CircleNode(
                circleId,
                "Accent Circle",
                new TransformValue(1240, 80, 1, 1, 0),
                null,
                true,
                false,
                new OpacityValue(0.24),
                new SizeValue(260, 260),
                new ColorValue(86, 166, 255),
                null)),
            new(titleId, new TextNode(
                titleId,
                "Slide Title",
                new TransformValue(120, 96, 1, 1, 0),
                null,
                true,
                false,
                OpacityValue.Full,
                "Why deterministic export matters",
                new RectValue(0, 0, 760, 64),
                new TypographyStyle("Inter", 48, 700, "start", 1, 0),
                new ColorValue(255, 255, 255))),
            new(bulletGroupId, new GroupNode(
                bulletGroupId,
                "Bullet Group",
                new TransformValue(120, 220, 1, 1, 0),
                null,
                true,
                false,
                OpacityValue.Full,
                new[] { panelId, bulletOneId, bulletTwoId, bulletThreeId })),
            new(panelId, new RectangleNode(
                panelId,
                "Bullet Panel",
                TransformValue.Identity,
                bulletGroupId,
                true,
                false,
                new OpacityValue(0.88),
                new SizeValue(960, 420),
                new ColorValue(33, 41, 60),
                new StrokeStyle(new ColorValue(86, 166, 255), 2),
                24)),
            new(bulletOneId, new TextNode(
                bulletOneId,
                "Bullet One",
                new TransformValue(48, 48, 1, 1, 0),
                bulletGroupId,
                true,
                false,
                OpacityValue.Full,
                "One scene graph drives editor, export, and agent context.",
                new RectValue(0, 0, 820, 48),
                new TypographyStyle("Inter", 28, 500, "start", 1.25, 0),
                new ColorValue(235, 240, 247))),
            new(bulletTwoId, new TextNode(
                bulletTwoId,
                "Bullet Two",
                new TransformValue(48, 144, 1, 1, 0),
                bulletGroupId,
                true,
                false,
                OpacityValue.Full,
                "Stable ordering turns export changes into intentional diffs.",
                new RectValue(0, 0, 820, 48),
                new TypographyStyle("Inter", 28, 500, "start", 1.25, 0),
                new ColorValue(235, 240, 247))),
            new(bulletThreeId, new TextNode(
                bulletThreeId,
                "Bullet Three",
                new TransformValue(48, 240, 1, 1, 0),
                bulletGroupId,
                true,
                false,
                OpacityValue.Full,
                "Transport-neutral commands keep human and agent edits aligned.",
                new RectValue(0, 0, 820, 48),
                new TypographyStyle("Inter", 28, 500, "start", 1.25, 0),
                new ColorValue(235, 240, 247))),
            new(footerLineId, new LineNode(
                footerLineId,
                "Footer Line",
                new TransformValue(120, 760, 1, 1, 0),
                null,
                true,
                false,
                OpacityValue.Full,
                new PointValue(0, 0),
                new PointValue(1360, 0),
                new StrokeStyle(new ColorValue(86, 166, 255), 3))),
        };

        return new DocumentState(
            DocumentId.From("doc_slide_export"),
            SchemaVersion.V1,
            "Export Stability Slide",
            new CanvasModel(1600, 900, CanvasPreset.Custom, new ColorValue(18, 22, 33), new SafeAreaInsets(80, 60, 80, 60)),
            ToDictionary(nodes, shuffled),
            new[] { circleId, titleId, bulletGroupId, footerLineId },
            new Dictionary<AssetId, AssetManifestEntry>());
    }

    private static IReadOnlyDictionary<TKey, TValue> ToDictionary<TKey, TValue>(
        IEnumerable<KeyValuePair<TKey, TValue>> entries,
        bool shuffled)
        where TKey : notnull
    {
        var orderedEntries = shuffled ? entries.Reverse() : entries;
        return orderedEntries.ToDictionary(entry => entry.Key, entry => entry.Value);
    }
}
