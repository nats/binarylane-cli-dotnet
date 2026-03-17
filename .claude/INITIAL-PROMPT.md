Create a .NET console program ("blnet") that is the equivalent of
../src/binarylane-cli ("blpy"):

 1. Use a prebuild step (like `task generate` in blpy) to generate source
    files based on openapi specification.

 2. blnet must be a dropin replacement for blpy in terms of command line
    arguments - command tree, option names, etc, support the same set of
    environment variables (`BL_CONTEXT`, `BL_API_TOKEN`, etc), same
    standardised options like --output json and --format col1,col2, and
    read/write the same configuration file at ~/.config/binarylane-cli/config.ini

 3. other than these preceding requirements, blnet should not be a direct "port"
    of blpy and instead should follow .NET development conventions and best
    practices (DI, unit tests, etc)

 4. use nuget packages where appropriate (e.g. command line parsing, stdout
    formatting) instead of hand-rolling functionality

 5. create integration test harness that executes blpy+blnet and verifies they
    behave the same - in particular the `--curl` option can be used to
    validate cmd line parsing is the same on both

 6. this is new project, blnet has no source code yet.
