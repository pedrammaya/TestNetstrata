# TestNetstrata
C# console app to convert a C# object definition to the Typescript counterpart.


# CSharpClassParser & TypeScriptConverter

## Overview
This project provides two related utilities:

- **CSharpClassParser** – Parses C# class source code into a structured representation.
- **TypeScriptConverter** – Converts the parsed representation into TypeScript interfaces.

---

## ✨ Features
### CSharpClassParser
- Parses **C# class source code** using regex.
- Extracts:
  - Class names
  - Properties (including nullable types, lists, and generics)
  - Nested classes (recursively)
- Produces a **tree structure** where parent classes hold references to nested classes.
- Handles type normalization (e.g., `Nullable<int>` vs `int?`).


---

### TypeScriptConverter
- Converts parsed C# classes into **TypeScript interfaces**.
- Features:
  - Property names converted to **camelCase**.
  - Type mapping:
    - `int`, `long` → `number`
    - `List<T>` → `T[]`
    - Nullable types → optional `?` in TypeScript
  - Avoids duplicate interface definitions.
  - Supports nested classes as separate interfaces.

---


## 🛠 How It Works
1. **Parsing**
   - Entry point: `ParseClasses(string input)`
   - Finds the first/top-level class with regex.
   - Delegates to `ParseClassAt` to:
     - Extract class body `{ ... }`
     - Detect and normalize properties
     - Recursively parse nested classes.

2. **Conversion**
   - Entry point: `ConvertClasses(CSharpClass rootClass)`
   - Emits TypeScript interface definitions for:
     - Root class
     - All nested classes
   - Ensures consistent type mappings and optional markers.


