import { HttpErrorResponse } from '@angular/common/http';

import { AiMatchConfidence, AiStatusDto } from '../dtos/ai.dto';

export const AI_NOT_CONFIGURED_MESSAGE =
  'A IA não está configurada neste servidor. Peça a quem administra o sistema para ativá-la.';

/** Formats a yyyy-MM-dd date as dd/MM/yyyy without timezone surprises. */
export function formatAiDate(isoDate: string): string {
  const [year, month, day] = isoDate.split('-');
  return year && month && day ? `${day}/${month}/${year}` : isoDate;
}

/** "3 de 20 usos de IA neste mês. Renova em 01/12/2026." */
export function describeAiUsage(status: AiStatusDto): string {
  return `${status.used} de ${status.limit} usos de IA neste mês. Renova em ${formatAiDate(status.renewsOn)}.`;
}

/** Why the AI buttons are disabled, or null when they can be used. */
export function aiUnavailableReason(status: AiStatusDto | null, statusFailed: boolean): string | null {
  if (statusFailed) return 'Não foi possível verificar a IA agora. Recarregue a página para tentar de novo.';
  if (!status) return 'Verificando a IA...';
  if (!status.enabled) return AI_NOT_CONFIGURED_MESSAGE;
  if (status.remaining <= 0) {
    return `Você já usou os ${status.limit} usos de IA deste mês. O limite renova em ${formatAiDate(status.renewsOn)}.`;
  }
  return null;
}

/** Message for the user after a failed AI call. Server messages for AI errors are already in Portuguese. */
export function aiErrorMessage(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) {
    return 'Não foi possível concluir agora. Tente novamente.';
  }

  const body = error.error as { title?: unknown; errorCode?: unknown } | null;
  const errorCode = typeof body?.errorCode === 'string' ? body.errorCode : '';
  const title = typeof body?.title === 'string' ? body.title : '';

  if (errorCode.startsWith('ai_') && title) return title;
  if (errorCode === 'rate_limited') return 'Muitas leituras em pouco tempo. Aguarde um minuto e tente de novo.';
  if (error.status === 0) return 'Sem conexão com o servidor. Confira sua internet e tente novamente.';
  if (error.status === 413) return 'O arquivo é grande demais. Envie um arquivo menor.';
  if (error.status === 404) return 'A lista não foi encontrada. Recarregue a página.';
  return 'Não foi possível concluir agora. Tente novamente.';
}

export const AI_CONFIDENCE_LABELS: Record<AiMatchConfidence, string> = {
  high: 'Alta',
  medium: 'Média',
  low: 'Baixa',
  none: 'Sem correspondência',
};

/** Accepts "24,90", "24.90", "1.234,56" and "R$ 5"; returns NaN for anything else. */
export function parseLocalizedPrice(value: string): number {
  const text = value.replace(/\s/g, '').replace(/^R\$/i, '');
  // Dots followed by three digits are thousand separators, as people type them in Brazil.
  if (/^\d{1,3}(\.\d{3})+(,\d+)?$/.test(text)) {
    return Number(text.replaceAll('.', '').replace(',', '.'));
  }
  if (/^\d+([.,]\d+)?$/.test(text)) return Number(text.replace(',', '.'));
  return NaN;
}

export function formatPriceInput(value: number): string {
  return value.toFixed(2).replace('.', ',');
}

export function formatFileSize(bytes: number): string {
  const megabytes = bytes / (1024 * 1024);
  return `${megabytes.toLocaleString('pt-BR', { maximumFractionDigits: 1 })} MB`;
}
