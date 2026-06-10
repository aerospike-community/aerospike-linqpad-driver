# Copilot Instructions

## Project Guidelines
- When AIContext markdown or C# AI context files change, increment AIContextVersion.cs and update version headers in the changed markdown files accordingly.

## AIContext Enhancements
- Add user-intent-overrides-defaults rule.
- Implement connection inference precedence policy.
- Include native-mode examples that preserve explicit requested policy values.

## LINQPad Specific Instructions
- In LINQPad expression mode, maintain the alias `using Exp = Aerospike.Client.Exp;` and do not rewrite it to `Aerospike.Exp`.