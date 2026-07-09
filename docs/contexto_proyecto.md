# Contexto del Proyecto: Sistema de Credenciales Académicas Descentralizadas

## Descripción General

Sistema basado en microservicios para la emisión, gestión y verificación de credenciales académicas digitales sobre blockchain. Utiliza identidad descentralizada (DIDs), firmas criptográficas (EIP-712), almacenamiento en IPFS y anclaje on-chain en la red Ethereum Sepolia.

---

## Arquitectura: Microservicios

### 1. `Auth.API` — Servicio de Autenticación

**Responsabilidad:** Proteger el ecosistema y validar la identidad criptográfica de quien se conecta.

| # | Funcionalidad | Descripción |
|---|---------------|-------------|
| 1 | Autenticación Descentralizada (SIWE) | Generación de *nonce* y validación de firma contra la red Ethereum |
| 2 | Gestión de Sesión (JWT) | Emisión de tokens de acceso con roles para el API Gateway |

---

### 2. `Identity.API` — Servicio de Gestión de Identidad

**Responsabilidad:** Administrar las entidades relacionales clásicas del sistema.

| # | Funcionalidad | Descripción |
|---|---------------|-------------|
| 3 | CRUD de Instituciones Académicas | Registro de nombre, DID asociado y estado |
| 4 | CRUD de Alumnos/Egresados | Registro de datos personales y vinculación de billetera/DID |

---

### 3. `Issuer.API` — Servicio Emisor

**Responsabilidad:** Motor core para la creación y registro en blockchain de los títulos académicos.

| # | Funcionalidad | Descripción |
|---|---------------|-------------|
| 5 | Generación de Credencial Verificable (VC) | Construcción del JSON-LD estándar W3C |
| 6 | Firma Criptográfica de Emisor | Firma institucional con EIP-712 |
| 7 | Almacenamiento Descentralizado | Subida del JSON a IPFS y obtención del CID |
| 8 | Anclaje On-Chain | Interacción con Nethereum para registrar el Hash SHA-256 en Sepolia |

---

### 4. `Verifier.API` — Servicio Verificador

**Responsabilidad:** Actuar como auditor criptográfico para validar documentos externos.

| # | Funcionalidad | Descripción |
|---|---------------|-------------|
| 9  | Validación de Integridad y Firma | Comprobar que el JSON-LD no esté alterado y que la firma EIP-712 sea legítima |
| 10 | Comprobación de Estado On-Chain | Lectura de Smart Contracts en Sepolia para confirmar existencia y estado de revocación |

---

## Funcionalidades Transversales (sin microservicio asignado aún)

Estas funciones fueron listadas como requerimientos pero no fueron distribuidas en un microservicio específico. Requieren decisión de arquitectura.

| Funcionalidad | Notas |
|---------------|-------|
| CRUD de Emisores | Posible pertenencia a `Identity.API` |
| CRUD de Usuarios | Posible pertenencia a `Identity.API` |
| CRUD de Carreras | Relacionado con `Identity.API` o un nuevo `Academic.API` |
| Control de Roles | Puede residir en `Auth.API` o como capa del API Gateway |
| Revocar / Congelar Credenciales | Candidato a `Issuer.API` o servicio independiente `Revocation.API` |
| Log de Accesos | Servicio transversal de auditoría |
| Métricas | Registro de consultas por institución y cantidad de certificados emitidos |

---

## Stack Tecnológico (inferido)

| Componente | Tecnología |
|------------|------------|
| Blockchain | Ethereum Sepolia (testnet) |
| Autenticación | SIWE (Sign-In with Ethereum) |
| Firma criptográfica | EIP-712 |
| Almacenamiento descentralizado | IPFS |
| Credenciales | W3C Verifiable Credentials (JSON-LD) |
| Interacción blockchain | Nethereum (.NET) |
| Sesiones | JWT |

---

## Pendientes / Decisiones Abiertas

- Asignar microservicio a las funcionalidades transversales listadas arriba.
- Definir si Métricas y Log de Accesos son parte de un microservicio existente o de una capa de observabilidad independiente.
- Definir estrategia de revocación on-chain (lista de revocación en Smart Contract).
- Decidir si Control de Roles vive en `Auth.API` o en el API Gateway.
