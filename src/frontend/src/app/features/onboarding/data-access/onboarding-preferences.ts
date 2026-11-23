/**
 * Guarda no navegador, por usuário, se a lista de primeiros passos foi dispensada
 * ou concluída. Sem acesso ao localStorage (modo privado, SSR, bloqueio), a lista
 * apenas volta a aparecer: nada quebra.
 */
export type OnboardingClosedReason = 'dismissed' | 'completed';

const STORAGE_KEY_PREFIX = 'planejador:onboarding-checklist:';

export function readOnboardingClosedReason(userId: string): OnboardingClosedReason | null {
  try {
    const value = globalThis.localStorage?.getItem(STORAGE_KEY_PREFIX + userId) ?? null;
    return value === 'dismissed' || value === 'completed' ? value : null;
  } catch {
    return null;
  }
}

export function saveOnboardingClosedReason(userId: string, reason: OnboardingClosedReason): void {
  try {
    globalThis.localStorage?.setItem(STORAGE_KEY_PREFIX + userId, reason);
  } catch {
    // Sem armazenamento disponível: a escolha vale só até recarregar a página.
  }
}
