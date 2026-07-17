# Copilot Instructions

## Project Guidelines
- When AIContext markdown or C# AI context files change, increment AIContextVersion.cs and update version headers in the changed markdown files accordingly.

## AIContext Enhancements
- Add user-intent-overrides-defaults rule.
- Implement connection inference precedence policy.
- Include native-mode examples that preserve explicit requested policy values.
- Never use dynamic fallback defaults for typed dictionaries (e.g., Dictionary<long, T> with List<dynamic> default).

## LINQPad Specific Instructions
- In LINQPad expression mode, maintain the alias `using Exp = Aerospike.Client.Exp;` and do not rewrite it to `Aerospike.Exp`.
- In LINQPad driver mode, prefer named record types over anonymous types; map record properties to set RecordCls bins, avoid unnecessary AValue-to-CLR conversions, and in native mode use CLR types instead of AValue.
- In generated LINQPad scripts, all record type declarations must be placed at the end of the script.