// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

/// @title CredentialRegistry — on-chain anchor for SovereignID academic credentials
/// @notice Stores content hashes and revocation state; full VC documents live on IPFS.
contract CredentialRegistry {
    struct CredentialRecord {
        bytes32 contentHash;
        string ipfsCid;
        bytes32 institutionId;
        address issuer;
        address subject;
        bool revoked;
    }

    address public owner;

    mapping(bytes32 => CredentialRecord) private _credentials;
    mapping(bytes32 => address) public institutionIssuers;

    event InstitutionIssuerRegistered(bytes32 indexed institutionId, address indexed issuerWallet);
    event CredentialRegistered(
        bytes32 indexed credentialId,
        bytes32 indexed institutionId,
        bytes32 contentHash,
        string ipfsCid,
        address issuer,
        address subject
    );
    event CredentialRevoked(bytes32 indexed credentialId, address indexed revoker);

    error NotInstitutionIssuer();
    error CredentialAlreadyExists();
    error CredentialNotFound();
    error CredentialAlreadyRevoked();
    error NotCredentialIssuer();
    error InstitutionIssuerAlreadySet();

    modifier onlyOwner() {
        require(msg.sender == owner, "Not owner");
        _;
    }

    constructor() {
        owner = msg.sender;
    }

    /// @notice Links an institution UUID (as bytes32) to the caller wallet (one-time self-registration).
    function registerInstitutionIssuer(bytes32 institutionId) external {
        if (institutionIssuers[institutionId] != address(0)) {
            revert InstitutionIssuerAlreadySet();
        }

        institutionIssuers[institutionId] = msg.sender;
        emit InstitutionIssuerRegistered(institutionId, msg.sender);
    }

    /// @notice Anchors a credential on-chain. Caller must be the registered issuer wallet for the institution.
    function registerCredential(
        bytes32 credentialId,
        bytes32 institutionId,
        bytes32 contentHash,
        string calldata ipfsCid,
        address subjectWallet
    ) external {
        if (institutionIssuers[institutionId] != msg.sender) {
            revert NotInstitutionIssuer();
        }

        if (_credentials[credentialId].issuer != address(0)) {
            revert CredentialAlreadyExists();
        }

        _credentials[credentialId] = CredentialRecord({
            contentHash: contentHash,
            ipfsCid: ipfsCid,
            institutionId: institutionId,
            issuer: msg.sender,
            subject: subjectWallet,
            revoked: false
        });

        emit CredentialRegistered(credentialId, institutionId, contentHash, ipfsCid, msg.sender, subjectWallet);
    }

    /// @notice Revokes a credential. Only the original issuer wallet may revoke.
    function revokeCredential(bytes32 credentialId) external {
        CredentialRecord storage record = _credentials[credentialId];

        if (record.issuer == address(0)) {
            revert CredentialNotFound();
        }

        if (record.revoked) {
            revert CredentialAlreadyRevoked();
        }

        if (record.issuer != msg.sender) {
            revert NotCredentialIssuer();
        }

        record.revoked = true;
        emit CredentialRevoked(credentialId, msg.sender);
    }

    function getCredential(bytes32 credentialId)
        external
        view
        returns (
            bytes32 contentHash,
            string memory ipfsCid,
            bytes32 institutionId,
            address issuer,
            address subject,
            bool revoked
        )
    {
        CredentialRecord storage record = _credentials[credentialId];
        if (record.issuer == address(0)) {
            revert CredentialNotFound();
        }

        return (
            record.contentHash,
            record.ipfsCid,
            record.institutionId,
            record.issuer,
            record.subject,
            record.revoked
        );
    }

    function isRevoked(bytes32 credentialId) external view returns (bool) {
        return _credentials[credentialId].revoked;
    }

    /// @notice Owner may reassign issuer wallet (e.g. wallet rotation) after off-chain verification.
    function setInstitutionIssuer(bytes32 institutionId, address issuerWallet) external onlyOwner {
        institutionIssuers[institutionId] = issuerWallet;
        emit InstitutionIssuerRegistered(institutionId, issuerWallet);
    }
}
