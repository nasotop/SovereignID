const hre = require("hardhat");
const fs = require("fs");
const path = require("path");

async function main() {
  const CredentialRegistry = await hre.ethers.getContractFactory("CredentialRegistry");
  const registry = await CredentialRegistry.deploy();
  await registry.waitForDeployment();

  const address = await registry.getAddress();
  const network = await hre.ethers.provider.getNetwork();

  const deployment = {
    contractName: "CredentialRegistry",
    address,
    chainId: Number(network.chainId),
    network: hre.network.name,
    deployedAt: new Date().toISOString(),
  };

  const outDir = path.join(__dirname, "..", "..", "docs", "contracts");
  fs.mkdirSync(outDir, { recursive: true });

  const fileName =
    Number(network.chainId) === 11155111
      ? "credential-registry.sepolia.json"
      : `credential-registry.${hre.network.name}.json`;

  fs.writeFileSync(path.join(outDir, fileName), JSON.stringify(deployment, null, 2));

  console.log(`CredentialRegistry deployed to ${address} on ${hre.network.name} (chainId ${network.chainId})`);
  console.log(`Deployment metadata written to docs/contracts/${fileName}`);
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
