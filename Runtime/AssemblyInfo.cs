using System.Runtime.CompilerServices;

// Editor tooling and tests may reach internal authoring surface (e.g. definition fields)
// that is deliberately kept out of the public runtime API.
[assembly: InternalsVisibleTo("toolbox.Editor")]
[assembly: InternalsVisibleTo("toolbox.Tests")]
