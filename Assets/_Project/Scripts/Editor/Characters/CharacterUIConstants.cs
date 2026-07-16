using UnityEngine;

namespace GlimmerOfHope.Editor.Characters
{
    /// <summary>
    /// Shared palette and layout constants for the Character Creator UI.
    /// Art Lead: change colors here to reskin the whole UI.
    /// </summary>
    public static class CharacterUIConstants
    {
        #region Palette — Warm Hope (high contrast)

        public static readonly Color TOPBAR          = new(0.255f, 0.506f, 0.475f);
        public static readonly Color TOPBAR_TEXT     = new(1f, 1f, 1f, 1f);
        public static readonly Color TOPBAR_SUB      = new(1f, 1f, 1f, 0.70f);

        public static readonly Color PANEL_BG        = new(0.945f, 0.937f, 0.922f);
        public static readonly Color PREVIEW_BG      = new(0.780f, 0.835f, 0.812f);
        public static readonly Color PREVIEW_OVERLAY  = new(0.780f, 0.835f, 0.812f, 0.30f);
        public static readonly Color CAMERA_BG        = new(0.690f, 0.773f, 0.745f);

        public static readonly Color ACCENT           = new(0.290f, 0.588f, 0.545f);
        public static readonly Color ACCENT_LIGHT     = new(0.820f, 0.930f, 0.906f);
        public static readonly Color ACCENT_DARK      = new(0.200f, 0.455f, 0.420f);
        public static readonly Color CONFIRM          = new(0.290f, 0.588f, 0.545f);

        public static readonly Color CARD_BG          = Color.white;
        public static readonly Color CARD_HOVER       = new(0.925f, 0.965f, 0.953f);
        public static readonly Color CARD_PRESSED     = new(0.855f, 0.918f, 0.898f);
        public static readonly Color CARD_SELECTED_BG = new(0.878f, 0.949f, 0.929f);

        public static readonly Color BTN_NEUTRAL_BG   = Color.white;
        public static readonly Color BTN_NEUTRAL_HOVER = new(0.933f, 0.925f, 0.910f);

        public static readonly Color TEXT_PRIMARY     = new(0.145f, 0.133f, 0.114f);
        public static readonly Color TEXT_MUTED       = new(0.420f, 0.400f, 0.365f);
        public static readonly Color TEXT_ON_ACCENT   = Color.white;

        public static readonly Color DIVIDER          = new(0.880f, 0.870f, 0.850f);
        public static readonly Color SHADOW_COLOR     = new(0f, 0f, 0f, 0.14f);
        public static readonly Color CARD_SHADOW      = new(0f, 0f, 0f, 0.10f);

        #endregion

        #region Layout

        public const float TOPBAR_HEIGHT    = 64f;
        public const float BOTTOMBAR_HEIGHT = 56f;
        public const float PANEL_SPLIT      = 0.575f;
        public const float PANEL_GAP        = 12f;

        #endregion

        #region Typography

        public const float FONT_TITLE       = 24f;
        public const float FONT_SUBTITLE    = 14f;
        public const float FONT_SECTION     = 14f;
        public const float FONT_BUTTON      = 15f;
        public const float FONT_STATUS      = 14f;

        #endregion

        #region Grid

        public const float GRID_CELL_SIZE   = 88f;
        public const float GRID_SPACING     = 12f;
        public const int   GRID_COLUMNS     = 3;
        public const float GRID_PADDING     = 16f;

        #endregion

        #region Sprites Path

        public const string SPRITES_PATH = "Assets/_Project/Art/Textures/UI/Characters";

        #endregion
    }
}
