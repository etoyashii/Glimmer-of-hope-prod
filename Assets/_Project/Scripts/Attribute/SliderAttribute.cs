using UnityEngine;

namespace GlimmerOfHope.Editor
{
    /// <summary>
    /// Affixhe un slider coloré sous le champ dans l'éditeur.
    /// Usage : [Slider(0f, 100f)] ou [Slider(0f, 100f, SliderColor.Red)]
    /// </summary>
    public enum SliderColor
    {
        Red,
        Green,
        Blue,
        Yellow,
        Cyan,
        Magenta,
        White,
        Black,
        Gray,
        Grey,
        Clear,
        Orange,
        Purple,
        Pink,
        Brown
    }

    public class SliderAttribute : PropertyAttribute
    {
        #region Properties
        public readonly float min;
        public readonly float max;
        public readonly Color color;
        #endregion

        #region Constructeurs
        public SliderAttribute(float _min, float _max, SliderColor _color)
        {
            this.min = _min;
            this.max = _max;

            color = _color switch
            {
                SliderColor.Red => Color.red,
                SliderColor.Green => Color.green,
                SliderColor.Blue => Color.blue,
                SliderColor.Yellow => Color.yellow,
                SliderColor.Cyan => Color.cyan,
                SliderColor.Magenta => Color.magenta,
                SliderColor.White => Color.white,
                SliderColor.Black => Color.black,
                SliderColor.Gray => Color.gray,
                SliderColor.Grey => Color.grey,
                SliderColor.Clear => Color.clear,
                SliderColor.Orange => new Color(1f, 0.5f, 0f),
                SliderColor.Purple => new Color(0.5f, 0f, 1f),
                SliderColor.Pink => new Color(1f, 0.4f, 0.7f),
                SliderColor.Brown => new Color(0.4f, 0.25f, 0.1f),

                _ => Color.white,
            };
        }

        public SliderAttribute(float _min, float _max)
            : this(_min, _max, SliderColor.Blue) { }

        public SliderAttribute(int _min, int _max, SliderColor _color)
        {
            this.min = _min;
            this.max = _max;

            color = _color switch
            {
                SliderColor.Red => Color.red,
                SliderColor.Green => Color.green,
                SliderColor.Blue => Color.blue,
                SliderColor.Yellow => Color.yellow,
                SliderColor.Cyan => Color.cyan,
                SliderColor.Magenta => Color.magenta,
                SliderColor.White => Color.white,
                SliderColor.Black => Color.black,
                SliderColor.Gray => Color.gray,
                SliderColor.Grey => Color.grey,
                SliderColor.Clear => Color.clear,
                SliderColor.Orange => new Color(1f, 0.5f, 0f),
                SliderColor.Purple => new Color(0.5f, 0f, 1f),
                SliderColor.Pink => new Color(1f, 0.4f, 0.7f),
                SliderColor.Brown => new Color(0.4f, 0.25f, 0.1f),

                _ => Color.white,
            };
        }

        public SliderAttribute(int _min, int _max)
        : this(_min, _max, SliderColor.Blue) { }
        #endregion
    }
}
