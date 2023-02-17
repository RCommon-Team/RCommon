# RCommon Application Framework

## Overview
RCommon was originally born as the (now abandoned) [NCommon](https://github.com/riteshrao/ncommon "NCommon") project but was resurrected out of the need to generate a productive, yet a relatively sound (architecturally speaking) application. Architectural patterns are used to implement some of the most commonly used tools in the .NET 7 stack. The primary goals of this framework are:
1. Future proofing applications against changing architectural or infrastructure needs.
2. Solve common problems under the presentation layer. Presentation frameworks are something else entirely. We try to keep everything nice under the hood. Cross cutting concerns, persistence strategies, transaction management, validation, business rules, exception management, and logging is where we want to shine.
3. Code testability. We try to limit the "magic" used. Things like dependency injection are used but in a very straightforward manner. Unit tests, and integration tests should be implemented to the highest degree possible. Afterall, we want the applications you build on top of this to work :) 
4. Last but not least - open source forever. 

We track bugs, enhancement requests, new feature requests, and general issues on [GitHub Issues](https://github.com/Reactor2Team/RCommon/issues "GitHub Issues") and are very responsive. General "how to" and community support should be managed on [Stack Overflow](https://stackoverflow.com/questions/tagged/rcommon "Stack Overflow"). 

## Documentation
We have begun maintaining and publishing our documentation at [https://docs.rcommon.com](https://docs.rcommon.com)
