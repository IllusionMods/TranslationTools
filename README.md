# TranslationTools
Translation tools for Illusion games

## Plugins

### TextResourceRedirector

- **[AI_TextResourceRedirector]** - For AI Girl
- **[HS_TextResourceRedirector]** - For Honey Select
- **[HS2_TextResourceRedirector]** - For Honey Select 2
- **[KK_TextResourceRedirector]** - For Koikatsu
- **[KKS_TextResourceRedirector]** - For Koikatsu Sunshine



Allows translations to override individual assets. Required for some translations to function correctly. Requires [XUnity.AutoTranslator](https://github.com/bbepis/XUnity.AutoTranslator) v4.16 or greater.

### TextDump

- **[AI_TextDump]** - For AI Girl
- **[AI_INT_TextDump]** - For AI Shoujo (Steam)
- **[HS_TextDump]** - For Honey Select
- **[HS2_TextDump]** - For Honey Select 2
- **[KK_TextDump]** - For Koikatsu
- **[KKP_TextDump]** - For Koikatsu Party
- **[KKS_TextDump]** - For Koikatsu Sunshine


Dumps untranslated text in to .txt files so that the lines can be used by translators working on the translation projects. Normally only executes under studio on initial load. Versions for localized games run under the main game and require multiple dump stages. Check the console and/or log file for specifics.

Marked incompatible with [XUnity.AutoTranslator and XUnity.ResourceRedirector](https://github.com/bbepis/XUnity.AutoTranslator), since if either of them are installed it may not dump the unmodified assets.

### TranslationSync

- **[KK_TranslationSync]** - For Koikatsu

A plugin for correctly formatting translation files. Corrects formatting and copies translations from one file to another for the same personality in case of duplicate entries. Used by translators working on the [Koikatsu Story Translation](https://github.com/IllusionMods/KoikatsuStoryTranslation) project. No need to download unless you're working on translations.

To use, open the plugin settings and set a personality, press the hotkey (default 0) to sync translations. Read your bepinex console or output_log.txt to see the changes made or any warnings and errors. Press alt+hotkey to force sync translation files in case of differing translations (warning: make backups first. It may not be obvious which translations are treated as the primary source). Press ctrl+hotkey to sync translations for all personalities (warning: very slow).

Only works on extracted files (not zip archives), and uses [XUnity.AutoTranslator](https://github.com/bbepis/XUnity.AutoTranslator) configuration to determine where files should be located.

### ModTextDump

- **[AI_ModTextDump]** - For AI Girl
- **[HS2_ModTextDump]** - For Honey Select 2
- **[KK_ModTextDump]** - For Koikatsu
- **[KKS_ModTextDump]** - For Koikatsu Sunshine


Dumps mod names into translation files. Will apply current translations while dumping. To use:
- go to plugin options
- set your filtering level (you may need to experiment depending on your source and target languages in [XUnity.AutoTranslator](https://github.com/bbepis/XUnity.AutoTranslator))
- set to enabled
- quit
- launch main game, wait for message telling you dump is complete
- launch studio, wait for message telling you dump is complete

## Tools

### MergeIntoDump

Command line utility to any lines found in a dump to a set of asset translation files.  Works only on extracted files, modifies output files in place (**back up your files before using**).

```
> MergeInfoDump.exe TextDump\RedirectedResources BepInEx\Translations\en\RedirectedResources
```

[//]: # (## Latest Links)

[AI_TextResourceRedirector]: https://github.com/IllusionMods/TranslationTools/releases/download/r23/AI_TextResourceRedirector.v1.4.4.3.zip "v1.4.4.3"
[HS_TextResourceRedirector]: https://github.com/IllusionMods/TranslationTools/releases/download/r10/HS_TextResourceRedirector.v1.4.1.1.zip "v1.4.1.1"
[HS2_TextResourceRedirector]: https://github.com/IllusionMods/TranslationTools/releases/download/r23/HS2_TextResourceRedirector.v1.4.4.3.zip "v1.4.4.3"
[KK_TextResourceRedirector]: https://github.com/IllusionMods/TranslationTools/releases/download/r23/KK_TextResourceRedirector.v1.4.4.3.zip "v1.4.4.3"
[KKS_TextResourceRedirector]: https://github.com/IllusionMods/TranslationTools/releases/download/r25/KKS_TextResourceRedirector.v1.4.5.5.zip "v1.4.5.5"
[AI_TextDump]: https://github.com/IllusionMods/TranslationTools/releases/download/r21/AI_TextDump.v1.4.4.3.zip "v1.4.4.3"
[AI_INT_TextDump]: https://github.com/IllusionMods/TranslationTools/releases/download/r21/AI_INT_TextDump.v1.4.4.3.zip "v1.4.4.3"
[HS_TextDump]: https://github.com/IllusionMods/TranslationTools/releases/download/r2/HS_TextDump.v1.1.zip "v1.1"
[HS2_TextDump]: https://github.com/IllusionMods/TranslationTools/releases/download/r21/HS2_TextDump.v1.4.4.3.zip "v1.4.4.3"
[KK_TextDump]: https://github.com/IllusionMods/TranslationTools/releases/download/r21/KK_TextDump.v1.4.4.3.zip "v1.4.4.3"
[KKS_TextDump]: https://github.com/IllusionMods/TranslationTools/releases/download/r24/KKS_TextDump.v1.4.5.2.zip "v1.4.5.2"
[KKP_TextDump]: https://github.com/IllusionMods/TranslationTools/releases/download/r15/KKP_TextDump.v1.4.3.zip "v1.4.3"
[KK_TranslationSync]: https://github.com/IllusionMods/TranslationTools/releases/download/r21/KK_TranslationSync.v1.3.3.3.zip "v1.3.3.3"
[AI_ModTextDump]: https://github.com/IllusionMods/TranslationTools/releases/download/r26/AI_ModTextDump.v0.7.0.1.zip "v0.7.0.1"
[HS2_ModTextDump]: https://github.com/IllusionMods/TranslationTools/releases/download/r26/HS2_ModTextDump.v0.7.0.1.zip "v0.7.0.1"
[KK_ModTextDump]: https://github.com/IllusionMods/TranslationTools/releases/download/r26/KK_ModTextDump.v0.7.0.1.zip "v0.7.0.1"
[KKS_ModTextDump]: https://github.com/IllusionMods/TranslationTools/releases/download/r26/KKS_ModTextDump.v0.7.0.1.zip "v0.7.0.1"

