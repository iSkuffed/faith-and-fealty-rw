Here's how it works:
Deity Name Generation System
The Chain
When IdeoFoundation_Deity generates a deity name, it uses this priority:
1. ideo.StructureMeme.deityNameMakerOverride  ← Our preset can override this
2. ideo.culture.deityNameMaker                ← Fallback to culture default
For the deity type (title like "Creator of everything", "all-powerful god"):
1. ideo.StructureMeme.deityTypeMakerOverride
2. ideo.culture.deityTypeMaker
How RulePackDefs Work
A RulePackDef defines grammar rules for procedural name generation. The system uses NameGenerator.GenerateName() which resolves a root rule (in this case r_deityName) by randomly picking from its expansions.
Example — NamerDeityChristian:
<RulePackDef>
    <defName>NamerDeityChristian</defName>
    <include>
        <li>GlobalUtility</li>
    </include>
    <rulePack>
        <rulesStrings>
            <li>r_deityName(p=3)->God</li>
            <li>r_deityName->Christ-Lord</li>
            <li>r_deityName->Christ</li>
            <li>r_deityName->[color] Jesus</li>
            <li>r_deityName->[color] Christ</li>
        </rulesStrings>
    </rulePack>
</RulePackDef>
- r_deityName(p=3)->God — 3x weight, produces "God"
- r_deityName->Christ-Lord — normal weight, produces "Christ-Lord"
- r_deityName->[color] Jesus — resolves [color] from GlobalUtility, produces e.g. "Golden Jesus"
Example — NamerDeityIslamic:
<rulesStrings>
    <li>r_deityName(p=8)->Allah</li>
    <li>r_deityName->Al-Ilah</li>
    <li>r_deityName->Ar-Rahman</li>
    ...
</rulesStrings>
Uses explicit name list with weighted selection (p=8 = 8x more likely).
Example — NamerDeityGlobal (generic fallback):
<rulesStrings>
    <li>r_deityName->[name]</li>
</rulesStrings>
Resolves [name] from GlobalUtility which uses syllable-based generation: [start][middle][end] with random consonant/vowel combinations.
How a Modder Would Create Their Own
Step 1: Define the RulePackDef in their mod's XML:
<RulePackDef>
    <defName>MyMod_NamerDeityCustom</defName>
    <include>
        <li>GlobalUtility</li>  <!-- Optional: include shared rules -->
    </include>
    <rulePack>
        <rulesStrings>
            <!-- Simple explicit names -->
            <li>r_deityName(p=5)->Zephyrus</li>
            <li>r_deityName(p=5)->Aether</li>
            <li>r_deityName(p=3)->The Nameless One</li>

            <!-- Procedural: prefix + suffix -->
            <li>r_deityName->[deo_prefix][deo_suffix]</li>
            <li>deo_prefix->Zeph</li>
            <li>deo_prefix->Ae</li>
            <li>deo_prefix->Thal</li>
            <li>deo_suffix->eros</li>
            <li>deo_suffix->ther</li>
            <li>deo_suffix->assus</li>

            <!-- Using GlobalUtility's [color] etc. -->
            <li>r_deityName->[color] Spirit</li>
        </rulesStrings>
    </rulePack>
</RulePackDef>
Step 2: Reference it in their ReligionPresetDef:
<IdeoRework.ReligionPresetDef>
    <defName>Preset_MyCustomReligion</defName>
    <baseName>Custom Faith</baseName>
    <structureMeme>Structure_OriginChristian</structureMeme>
    <deityNameMakerOverride>MyMod_NamerDeityCustom</deityNameMakerOverride>
</IdeoRework.ReligionPresetDef>