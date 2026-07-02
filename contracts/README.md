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

### Pinata (optional)

```js
localStorage.setItem('sovereignid.pinata.jwt', '<pinata-jwt>');
```

Without Pinata JWT, the frontend uses a deterministic dev CID for local demos.
