const fs = require("fs");
const path = require("path");

const artifactPath = path.join(
  __dirname,
  "..",
  "artifacts",
  "contracts",
  "CredentialRegistry.sol",
  "CredentialRegistry.json",
);

const webAbiPath = path.join(
  __dirname,
  "..",
  "..",
  "src",
  "web",
  "src",
  "app",
  "core",
  "contracts",
  "credential-registry.abi.json",
);

const docsAbiPath = path.join(
  __dirname,
  "..",
  "..",
  "docs",
  "contracts",
  "credential-registry.abi.json",
);

if (!fs.existsSync(artifactPath)) {
  console.error("Artifact not found. Run `npm run compile` first.");
  process.exit(1);
}

const artifact = JSON.parse(fs.readFileSync(artifactPath, "utf8"));
const abi = artifact.abi;

for (const target of [webAbiPath, docsAbiPath]) {
  fs.mkdirSync(path.dirname(target), { recursive: true });
  fs.writeFileSync(target, JSON.stringify(abi, null, 2));
  console.log(`Wrote ABI to ${target}`);
}
