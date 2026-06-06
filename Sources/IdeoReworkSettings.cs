using Verse;

namespace IdeoRework
{
    public class IdeoReworkSettings : ModSettings
    {
        public bool enableCognitiveDissonance = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enableCognitiveDissonance, "enableCognitiveDissonance", true);
            base.ExposeData();
        }
    }
}
