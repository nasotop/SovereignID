# SovereignID Smart Contracts

## CredentialRegistry

On-chain anchor for academic credentials. Stores `contentHash`, `ipfsCid`, issuer/subject wallets and revocation state.

### Commands

```bash
cd contracts
npm install
npm run compile
npm test
npm run export-abi
```

### Deploy Sepolia

```bash
export SEPOLIA_RPC_URL=https://rpc.sepolia.org
export DEPLOYER_PRIVATE_KEY=0x...
npm run deploy:sepolia
```

Deployment metadata is written to `docs/contracts/credential-registry.sepolia.json`.

Set the address in the Angular app:

```js
localStorage.setItem('sovereignid.registry.address', '<deployed-address>');
```

### IPFS / Pinata

Pinning is server-side only (`issuer-api` via `Issuer__ContentAnchor__*`). See [`docs/deployment.md`](../docs/deployment.md). Do not configure Pinata JWTs in the browser.
