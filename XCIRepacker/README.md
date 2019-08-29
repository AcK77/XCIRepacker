# XCIRepacker

This tool can convert your own XCI dumps into header patched NSPs who can be installed (with the content type patches for FS system module).

Before any explanations:

**I'M NOT SUPPORT PIRACY, THIS PATCH AND THIS TOOL ARE FOR INSTALL YOUR OWN GAMECARD DUMP!**

**THIS PATCH AND THE TOOL ARE PROVIDED AS-IS AND I'M NOT RESPONSIBLE OF ANY DAMAGES!**

- First you have to dump own your gamecard using [nxdumptool by DarkMatterCore](https://github.com/DarkMatterCore/nxdumptool) to get a XCI dump.
- Then you have to convert your XCI dump with XCIRepacker into a header patched NSP file (Usage below).
- Install the content type patch for Atmosphère or Hekate (Explanation below).
- Install the converted NSP using [Goldleaf by XorTroll](https://github.com/XorTroll/Goldleaf).
- Enjoy...

# Tool Usage

- `XCIRepacker.exe "PathOfFile.xci"`

# Content Type Patches installation

 (1.0.0 - 8.1.0 | FAT - ExFat)

- For Atmosphère: 

Put the `Patches\atmosphere` content inside `sdmc:/atmosphere` and reboot your switch.

- For Hekate: 

Put the `Patches\bootloader` content inside `sdmc:/bootloader` and reboot your switch.

![XCIRepacker](https://i.imgur.com/CfKMn6vl.png)

> Provide with the courtesy of the mob.
