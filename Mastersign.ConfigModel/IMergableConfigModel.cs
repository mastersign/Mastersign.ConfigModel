namespace Mastersign.ConfigModel
{
    public interface IMergableConfigModel
    {
        void UpdateWith(object source, bool forceDeepMerge);
    }
}
