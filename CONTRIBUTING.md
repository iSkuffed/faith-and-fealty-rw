## CONTRIBUTION GUIDELINES AND NOTICES ##

Thanks for checking out the project. This is a complex mod built on a specific vision by a single person (Skuffed/iSkuffed). 
To keep development moving forward and ensure the mod remains stable, I operate under strict guidelines for bug reports and contributions, which are detailed below.

## Bug Reports: ##

All bug reports must be submitted via the GitHub Issue Tracker.

When submitting an issue, you are required to provide:

    The Full Log: You must include your entire Player.log file. Use the "Copy" button in the RimWorld debug logger. Issues submitted without a full log will be closed immediately without investigation, not because I don't care, rather it makes solving your issue significantly harder if not impossible.

    Reproduction Steps: Provide clear, actionable steps on how to trigger it, if possible. The more information you provide, the more likely it is that your issue will be fixed!

    Mod List: A complete list of other mods present in your load order to help identify conflicts. Mod conflicts will be considered of the lowest priority as I just can't be expected to fix every conflict. I've done my absolute best (as described in the next section) to make the creation of patches as simple as possible. If you made a patch for a mod, submit a pull request! We'll add it into the integrated patches!

## Compatibility & Patches ##

The system is designed to be highly modular via XML patching. If you are requesting compatibility for another mod, we hope you'll consider making your own patch and submitting a pull request to have it implemented

    Develop the Patch: Use the existing architecture to define the necessary foundations, beliefs, and precepts for the target mod.

    Submit a Pull Request (PR): If your patch is functional and adheres to the project structure, submit a PR for review.

    Local Implementation: If you are unwilling or unable to create a patch, the source code is public. You are free to fork the repository and implement the changes for your own personal use. I only politely ask that you not reupload my mod in its entirety to Workshop, GitHub, etc. If you contribute to patches you will be credited in the CONTRIBUTORS.md file. If you contributed and you don't see your name added to there, let me know! Though keep in mind if your patch breaks and has to be repaired significantly or removed, I will unfortunately have to remove you from CONTRIBUTORS to keep it clean.

## Code Submission Requirements ##

All code contributions must adhere to the following standards to ensure the security and stability of the project:

    Source Code Only (Security Policy): I maintain a strict source-only policy. Under no circumstances will I accept pre-compiled binary files or .dll files. Any Pull Request (PR) containing binary blobs will be rejected and closed immediately without review. I will not decompile unknown binaries to verify them; if you want your code considered, provide the source.

    Maintainer Compilation: All final builds and compilation processes are handled exclusively by me (Skuffed/iSkuffed). This ensures that the code running in the final released version is exactly what I have audited and approved. In order to assure quality and security for all users of this mod.

    Documentation: Every Pull Request is expected to provide a clear, concise description of the changes, the rationale behind them, and instructions on how to test the implementation. Contributors who do not do so should expect their PR to be denied.

    Review Process: I will review all submitted source code for architectural consistency and security risks. I may reach out to inquire about changes made, if you wish to provide another source of contact (other than GitHub) I accept Discord handles as well. (No Telegram/Phone Numbers/E-Mails etc.)

## Licensing and Legal ##

    License Compliance: This project is licensed under the GNU GPL v2. By submitting a Pull Request, you explicitly acknowledge and agree that all contributed content is licensed under the same terms.

    No Exceptions: I do not accept contributions under proprietary, restrictive, or "All Rights Reserved" licenses. If you are not willing to share your contribution under the GNU GPL v2, do not submit it.

    Integrity of Contribution: By submitting a contribution, you grant me (the project maintainer) the irrevocable right to modify, compile, and distribute your code as part of this project, consistent with the terms of the GNU GPL v2. Any request to remove contributions made to this project after a PR is accepted will be denied.

## Code Contributions ##

If you wish to contribute code:

    Open an Issue first: Discuss your proposed changes before writing code. I am the sole maintainer of this project, and I have a specific vision and scope for the project. If you wish to extend the scope of the project consider making a submod!

    Maintain Standards: Ensure your code is clean, documented, and consistent with the existing codebase. Comments are always encouraged!
    
    Lastly, Thanks for considering to contribute or contrbuting. We appreciate it.
