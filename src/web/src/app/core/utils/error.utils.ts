import { HttpErrorResponse } from '@angular/common/http';

import { isProblemDetails, ProblemDetails } from '../models/problem-details.models';

const LANGUAGE_STORAGE_KEY = 'sovereignid.language';

type AppLanguage = 'en' | 'es';
type LocalizedMessage = Record<AppLanguage, string>;

const GENERIC_MESSAGES: Record<string, LocalizedMessage> = {
  unknown: {
    en: 'Something went wrong. Please try again.',
    es: 'Algo salio mal. Intentalo nuevamente.',
  },
  network: {
    en: 'Could not connect to the service. Check your connection and try again.',
    es: 'No se pudo conectar con el servicio. Revisa tu conexion e intentalo nuevamente.',
  },
  badRequest: {
    en: 'Some data is invalid. Review the form and try again.',
    es: 'Algunos datos no son validos. Revisa el formulario e intentalo nuevamente.',
  },
  unauthorized: {
    en: 'Your session expired. Sign in again to continue.',
    es: 'Tu sesion expiro. Inicia sesion nuevamente para continuar.',
  },
  forbidden: {
    en: 'You do not have permission to perform this action.',
    es: 'No tienes permisos para realizar esta accion.',
  },
  notFound: {
    en: 'The requested information was not found.',
    es: 'No se encontro la informacion solicitada.',
  },
  conflict: {
    en: 'The action could not be completed because the data is not in the expected state.',
    es: 'La accion no se pudo completar porque los datos no estan en el estado esperado.',
  },
  rateLimited: {
    en: 'Too many requests. Wait a moment and try again.',
    es: 'Demasiadas solicitudes. Espera un momento e intentalo nuevamente.',
  },
  server: {
    en: 'The service is temporarily unavailable. Please try again later.',
    es: 'El servicio no esta disponible temporalmente. Intentalo mas tarde.',
  },
};

const ERROR_CODE_MESSAGES: Record<string, LocalizedMessage> = {
  siwe_parse_failed: {
    en: 'The wallet signature could not be read. Try signing in again.',
    es: 'No se pudo leer la firma de la wallet. Intenta iniciar sesion nuevamente.',
  },
  unsupported_chain: {
    en: 'Switch your wallet to the supported network and try again.',
    es: 'Cambia tu wallet a la red soportada e intentalo nuevamente.',
  },
  nonce_unknown: GENERIC_MESSAGES['unauthorized'],
  nonce_expired: GENERIC_MESSAGES['unauthorized'],
  nonce_consumed: GENERIC_MESSAGES['unauthorized'],
  signature_mismatch: {
    en: 'The wallet signature does not match the request. Try signing in again.',
    es: 'La firma de la wallet no coincide con la solicitud. Intenta iniciar sesion nuevamente.',
  },
  invalid_credential_id: {
    en: 'The credential identifier is not valid.',
    es: 'El identificador de la credencial no es valido.',
  },
  rate_limit_exceeded: GENERIC_MESSAGES['rateLimited'],
  invalid_report_period: {
    en: 'The selected reporting period is not valid.',
    es: 'El periodo seleccionado para el reporte no es valido.',
  },
  institution_not_found: {
    en: 'The institution was not found.',
    es: 'No se encontro la institucion.',
  },
  institution_code_exists: {
    en: 'An institution with that code already exists.',
    es: 'Ya existe una institucion con ese codigo.',
  },
  career_not_found: {
    en: 'The career was not found.',
    es: 'No se encontro la carrera.',
  },
  student_not_found: {
    en: 'The student was not found.',
    es: 'No se encontro el estudiante.',
  },
  credential_not_found: {
    en: 'The credential was not found.',
    es: 'No se encontro la credencial.',
  },
  issuer_wallet_not_found: {
    en: 'The issuer wallet is not linked to the institution.',
    es: 'La wallet emisora no esta vinculada a la institucion.',
  },
  institution_user_not_found: {
    en: 'The institution user was not found.',
    es: 'No se encontro el usuario institucional.',
  },
  invalid_institution: {
    en: 'The selected institution is not valid.',
    es: 'La institucion seleccionada no es valida.',
  },
  invalid_career: {
    en: 'The career data is not valid.',
    es: 'Los datos de la carrera no son validos.',
  },
  career_code_exists: {
    en: 'A career with that code already exists for this institution.',
    es: 'Ya existe una carrera con ese codigo en esta institucion.',
  },
  invalid_student: {
    en: 'The selected student is not valid.',
    es: 'El estudiante seleccionado no es valido.',
  },
  student_external_reference_exists: {
    en: 'A student with that reference already exists for this institution.',
    es: 'Ya existe un estudiante con esa referencia en esta institucion.',
  },
  invalid_wallet_address: {
    en: 'The wallet address is not valid.',
    es: 'La direccion de wallet no es valida.',
  },
  invalid_holder_wallet: {
    en: 'The holder wallet is not valid.',
    es: 'La wallet del holder no es valida.',
  },
  invalid_issuer_wallet: {
    en: 'The issuer wallet is not valid.',
    es: 'La wallet emisora no es valida.',
  },
  invalid_contact_email: {
    en: 'The contact email is not valid.',
    es: 'El email de contacto no es valido.',
  },
  invalid_invitation_email: {
    en: 'The invitation email is not valid.',
    es: 'El email de invitacion no es valido.',
  },
  invalid_birth_date: {
    en: 'The birth date is not valid.',
    es: 'La fecha de nacimiento no es valida.',
  },
  invalid_country_code: {
    en: 'The country code is not valid.',
    es: 'El codigo de pais no es valido.',
  },
  invalid_institution_role: {
    en: 'The selected role is not valid.',
    es: 'El rol seleccionado no es valido.',
  },
  invalid_invitation_token: {
    en: 'The invitation link is not valid.',
    es: 'El enlace de invitacion no es valido.',
  },
  invitation_not_usable: {
    en: 'The invitation is no longer available.',
    es: 'La invitacion ya no esta disponible.',
  },
  invitation_wallet_email_mismatch: {
    en: 'The selected wallet does not match the invitation.',
    es: 'La wallet seleccionada no coincide con la invitacion.',
  },
  invalid_credential_type: {
    en: 'The credential type is not valid.',
    es: 'El tipo de credencial no es valido.',
  },
  invalid_title_payload: {
    en: 'The title data is incomplete or invalid.',
    es: 'Los datos del titulo estan incompletos o no son validos.',
  },
  title_link_failed: {
    en: 'The title could not be linked. Check student, career, wallet and credential type data.',
    es: 'No se pudo vincular el titulo. Revisa estudiante, carrera, wallet y tipo de credencial.',
  },
  issuer_wallet_link_failed: {
    en: 'The issuer wallet could not be linked to the institution.',
    es: 'No se pudo vincular la wallet emisora a la institucion.',
  },
  invalid_credential: {
    en: 'The selected credential is not valid.',
    es: 'La credencial seleccionada no es valida.',
  },
  invalid_revocation_reason: {
    en: 'Enter a valid revocation reason.',
    es: 'Ingresa un motivo de revocacion valido.',
  },
  invalid_revocation_payload: {
    en: 'The revocation data is incomplete or invalid.',
    es: 'Los datos de revocacion estan incompletos o no son validos.',
  },
  credential_not_active: {
    en: 'Only active credentials can be revoked.',
    es: 'Solo se pueden revocar credenciales activas.',
  },
  revocation_failed: {
    en: 'The credential could not be revoked. Try again.',
    es: 'No se pudo revocar la credencial. Intentalo nuevamente.',
  },
  blockchain_tx_not_found: {
    en: 'The blockchain transaction could not be verified.',
    es: 'No se pudo verificar la transaccion en blockchain.',
  },
  blockchain_revocation_tx_not_found: {
    en: 'The blockchain revocation transaction could not be verified.',
    es: 'No se pudo verificar la transaccion de revocacion en blockchain.',
  },
  blockchain_issuer_mismatch: {
    en: 'The blockchain issuer does not match the institution.',
    es: 'El emisor registrado en blockchain no coincide con la institucion.',
  },
  blockchain_block_mismatch: {
    en: 'The blockchain block does not match the registered proof.',
    es: 'El bloque en blockchain no coincide con la prueba registrada.',
  },
  invalid_blockchain_proof: {
    en: 'The blockchain proof is not valid.',
    es: 'La prueba blockchain no es valida.',
  },
  already: {
    en: 'This record already exists or was already processed.',
    es: 'Este registro ya existe o ya fue procesado.',
  },
  expired: {
    en: 'This record has expired.',
    es: 'Este registro expiro.',
  },
};

const KNOWN_LOCAL_MESSAGES: Record<string, LocalizedMessage> = {
  'Failed to connect wallet': {
    en: 'Could not connect the wallet.',
    es: 'No se pudo conectar la wallet.',
  },
  'Could not connect the wallet': {
    en: 'Could not connect the wallet.',
    es: 'No se pudo conectar la wallet.',
  },
  'No se pudo conectar la wallet': {
    en: 'Could not connect the wallet.',
    es: 'No se pudo conectar la wallet.',
  },
  'Login failed': {
    en: 'Could not sign in. Try again.',
    es: 'No se pudo iniciar sesion. Intentalo nuevamente.',
  },
  'Failed to sign message': {
    en: 'Could not sign the message with your wallet.',
    es: 'No se pudo firmar el mensaje con tu wallet.',
  },
  'Failed to fetch nonce from server': {
    en: 'Could not start the sign-in request. Try again.',
    es: 'No se pudo iniciar la solicitud de acceso. Intentalo nuevamente.',
  },
  'Signature verification failed on server': {
    en: 'Could not verify the wallet signature.',
    es: 'No se pudo verificar la firma de la wallet.',
  },
  'MetaMask or Ethereum provider not detected': {
    en: 'MetaMask is not available in this browser.',
    es: 'MetaMask no esta disponible en este navegador.',
  },
  'Wallet returned an invalid chain ID.': {
    en: 'The wallet returned an invalid network.',
    es: 'La wallet retorno una red no valida.',
  },
  'Wallet provider is not available.': {
    en: 'Wallet provider is not available.',
    es: 'El proveedor de wallet no esta disponible.',
  },
  'Provider not initialized': {
    en: 'Wallet provider is not available.',
    es: 'El proveedor de wallet no esta disponible.',
  },
  'Storage is not available': {
    en: 'Browser storage is not available.',
    es: 'El almacenamiento del navegador no esta disponible.',
  },
  'Pinata pinning failed.': {
    en: 'Could not upload the credential metadata.',
    es: 'No se pudo subir la metadata de la credencial.',
  },
  'El portapapeles no está disponible en este navegador.': {
    en: 'Clipboard is not available in this browser.',
    es: 'El portapapeles no esta disponible en este navegador.',
  },
  'Demasiadas solicitudes. Espera un momento e inténtalo de nuevo.': {
    en: 'Too many requests. Wait a moment and try again.',
    es: 'Demasiadas solicitudes. Espera un momento e intentalo nuevamente.',
  },
  'Verification failed': {
    en: 'The credential could not be verified. Try again.',
    es: 'No se pudo verificar la credencial. Intentalo nuevamente.',
  },
  'No se pudo aceptar la invitacion': {
    en: 'The invitation could not be accepted. Try again.',
    es: 'No se pudo aceptar la invitacion. Intentalo nuevamente.',
  },
};

function currentLanguage(): AppLanguage {
  try {
    return localStorage.getItem(LANGUAGE_STORAGE_KEY) === 'es' ? 'es' : 'en';
  } catch {
    return 'en';
  }
}

function pick(message: LocalizedMessage): string {
  return message[currentLanguage()];
}

function messageForStatus(status: number): string {
  if (status === 0) {
    return pick(GENERIC_MESSAGES['network']);
  }

  if (status === 400 || status === 422) {
    return pick(GENERIC_MESSAGES['badRequest']);
  }

  if (status === 401) {
    return pick(GENERIC_MESSAGES['unauthorized']);
  }

  if (status === 403) {
    return pick(GENERIC_MESSAGES['forbidden']);
  }

  if (status === 404) {
    return pick(GENERIC_MESSAGES['notFound']);
  }

  if (status === 409) {
    return pick(GENERIC_MESSAGES['conflict']);
  }

  if (status === 429) {
    return pick(GENERIC_MESSAGES['rateLimited']);
  }

  if (status >= 500) {
    return pick(GENERIC_MESSAGES['server']);
  }

  return pick(GENERIC_MESSAGES['unknown']);
}

function localizeKnownMessage(message: string): string | null {
  return KNOWN_LOCAL_MESSAGES[message]?.[currentLanguage()] ?? null;
}

function codeFromProblemDetails(problem: ProblemDetails): string | null {
  return problem.error?.trim() || null;
}

function messageForProblemDetails(problem: ProblemDetails, fallback: string): string {
  const code = codeFromProblemDetails(problem);
  if (code && ERROR_CODE_MESSAGES[code]) {
    return pick(ERROR_CODE_MESSAGES[code]);
  }

  if (problem.status) {
    return messageForStatus(problem.status);
  }

  return localizeKnownMessage(fallback) ?? fallback;
}

function isHttpFailureMessage(message: string): boolean {
  return message.startsWith('Http failure response');
}

function isUnsupportedNetworkMessage(message: string): boolean {
  return message.startsWith('Unsupported network');
}

/** Narrows unknown catch/observable errors to a human-readable message */
export function toErrorMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    return toHttpErrorMessage(error, pick(GENERIC_MESSAGES['unknown']));
  }

  if (error instanceof Error) {
    const known = localizeKnownMessage(error.message);
    if (known) {
      return known;
    }

    if (isUnsupportedNetworkMessage(error.message)) {
      return pick(ERROR_CODE_MESSAGES['unsupported_chain']);
    }

    return isHttpFailureMessage(error.message)
      ? pick(GENERIC_MESSAGES['unknown'])
      : pick(GENERIC_MESSAGES['unknown']);
  }

  if (typeof error === 'string') {
    return localizeKnownMessage(error) ?? pick(GENERIC_MESSAGES['unknown']);
  }

  return pick(GENERIC_MESSAGES['unknown']);
}

/** Maps HTTP and generic errors to a stable user-facing message */
export function toHttpErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof HttpErrorResponse) {
    const body = error.error;

    if (isProblemDetails(body)) {
      return messageForProblemDetails(body, fallback);
    }

    return messageForStatus(error.status);
  }

  return toErrorMessage(error) || localizeKnownMessage(fallback) || fallback;
}

/** Wraps unknown errors into Error instances for consistent propagation */
export function toThrownError(error: unknown, fallback: string): Error {
  if (error instanceof HttpErrorResponse) {
    return new Error(toHttpErrorMessage(error, fallback));
  }

  if (error instanceof Error) {
    return new Error(toErrorMessage(error));
  }

  return new Error(toHttpErrorMessage(error, fallback));
}

/** Stable machine-readable error code from Problem Details, if present */
export function toErrorCode(error: unknown): string | null {
  if (error instanceof HttpErrorResponse && isProblemDetails(error.error)) {
    return error.error.error ?? null;
  }

  return null;
}
