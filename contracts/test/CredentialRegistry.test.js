const { expect } = require("chai");
const { ethers } = require("hardhat");

function guidToBytes32(guid) {
  return "0x" + guid.replace(/-/g, "").padStart(64, "0");
}

describe("CredentialRegistry", function () {
  const institutionId = guidToBytes32("11111111-1111-1111-1111-111111111111");
  const credentialId = guidToBytes32("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
  const contentHash =
    "0xbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

  it("registers institution issuer and credential, then revokes", async function () {
    const [issuer, subject] = await ethers.getSigners();
    const Registry = await ethers.getContractFactory("CredentialRegistry");
    const registry = await Registry.deploy();

    await registry.connect(issuer).registerInstitutionIssuer(institutionId);

    await expect(
      registry
        .connect(issuer)
        .registerCredential(
          credentialId,
          institutionId,
          contentHash,
          "bafybeigdyrzt",
          subject.address,
        ),
    ).to.emit(registry, "CredentialRegistered");

    const record = await registry.getCredential(credentialId);
    expect(record.contentHash).to.equal(contentHash);
    expect(record.revoked).to.equal(false);

    await expect(registry.connect(issuer).revokeCredential(credentialId)).to.emit(
      registry,
      "CredentialRevoked",
    );

    expect(await registry.isRevoked(credentialId)).to.equal(true);
  });
});
