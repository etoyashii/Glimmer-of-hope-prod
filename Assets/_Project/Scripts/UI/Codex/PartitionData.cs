using UnityEngine;

namespace GlimmerOfHope.UI.BookMenu.Data
{
    public enum NoteColor { Red, Blue, Yellow, Black }

    [System.Serializable]
    public class PartitionData
    {
        public string Title;

        [TextArea]
        public string Description;

        [Tooltip("Static image or first frame of an animation")]
        public Sprite Thumbnail;

        public NoteColor[] Sequence;
    }
}