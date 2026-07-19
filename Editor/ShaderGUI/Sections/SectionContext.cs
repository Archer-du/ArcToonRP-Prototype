namespace ArcToon.Editor.ShaderEditor.Sections
{
    // Shared per-frame state that every section can read.
    // Populated by upstream sections (e.g. RegionIDSection) and consumed by downstream sections
    // (e.g. GeometryOutlineSection reading the active region). All fields default to a value
    // that behaves as if the feature were disabled, so sections that never touch the context
    // continue to work unchanged.
    public class SectionContext
    {
        // Active region index chosen in the RegionID section header. Downstream sections
        // that expose per-region parameters use this to select which slot to draw.
        public int SelectedRegion;

        public void Reset()
        {
            SelectedRegion = 0;
        }
    }
}
