using GlimmerOfHope.Core.Services;

namespace GlimmerOfHope.Core.Save
{
    public interface ISaveService : IService
    {
        SaveData CurrentSave { get; }
        bool HasSave { get; }
        void Save();
    }
}
