# LiteWebCompiler
 A simple semi-static webpage compiler
 
 Based on the idea of not having bulky, always-live compilers like Node.js to all the work, and replacing the XML with simple extendible Wikitext-style tags

 ## Demo
 1. Go to test.txt and see the markup. Maybe also see the markup of other files particularly referenced by the file. (The documentation in Interpreter.cs might help)
 2. Go into the CLI and type @run test.txt (Make sure the file is in the debug folder with the executable).
 3. The CLI will spit out its workings-out and some issues from it. Notice the compile errors and the output file with an ambiguous navbar at the bottom. Type @let page 2.
 4. Type @run test.txt. Most of the compile errors should disappear and the navbar template should highlight tab 2.
