import { HttpErrorResponse } from '@angular/common/http';

import { AiStatusDto } from '../dtos/ai.dto';
import {
  aiErrorMessage,
  aiUnavailableReason,
  describeAiUsage,
  formatPriceInput,
  parseLocalizedPrice,
} from './ai-support';

const STATUS: AiStatusDto = {
  enabled: true,
  used: 3,
  limit: 20,
  remaining: 17,
  renewsOn: '2026-12-01',
  maxFileBytes: 5242880,
  maxInputCharacters: 20000,
};

describe('ai-support', () => {
  it('describes the monthly usage', () => {
    expect(describeAiUsage(STATUS)).toBe('3 de 20 usos de IA neste mês. Renova em 01/12/2026.');
  });

  it('explains why the AI buttons are disabled', () => {
    expect(aiUnavailableReason(STATUS, false)).toBeNull();
    expect(aiUnavailableReason(null, false)).toContain('Verificando');
    expect(aiUnavailableReason(null, true)).toContain('Não foi possível verificar');
    expect(aiUnavailableReason({ ...STATUS, enabled: false }, false)).toContain(
      'não está configurada',
    );
    expect(aiUnavailableReason({ ...STATUS, used: 20, remaining: 0 }, false)).toBe(
      'Você já usou os 20 usos de IA deste mês. O limite renova em 01/12/2026.',
    );
  });

  it('shows the server message for AI errors and a generic one otherwise', () => {
    const limit = new HttpErrorResponse({
      status: 429,
      error: { title: 'Você já usou os 20 usos de IA deste mês.', errorCode: 'ai_usage_limit_exceeded' },
    });
    const burst = new HttpErrorResponse({
      status: 429,
      error: { title: 'Too many requests.', errorCode: 'rate_limited' },
    });
    const internal = new HttpErrorResponse({
      status: 500,
      error: { title: 'An unexpected error occurred.', errorCode: 'internal_server_error' },
    });

    expect(aiErrorMessage(limit)).toBe('Você já usou os 20 usos de IA deste mês.');
    expect(aiErrorMessage(burst)).toContain('Aguarde um minuto');
    expect(aiErrorMessage(internal)).toBe('Não foi possível concluir agora. Tente novamente.');
    expect(aiErrorMessage(new HttpErrorResponse({ status: 0 }))).toContain('Sem conexão');
    expect(aiErrorMessage(new Error('x'))).toBe('Não foi possível concluir agora. Tente novamente.');
  });

  it.each([
    ['24,90', 24.9],
    ['24.90', 24.9],
    ['R$ 1.234,56', 1234.56],
    ['1.234', 1234],
    ['5', 5],
    [' 0,5 ', 0.5],
  ])('parses the price %s', (text, expected) => {
    expect(parseLocalizedPrice(text)).toBe(expected);
  });

  it.each(['', 'abc', '-5', '1,2,3', '12,', 'R$'])('rejects the price "%s"', (text) => {
    expect(parseLocalizedPrice(text)).toBeNaN();
  });

  it('formats prices for editing', () => {
    expect(formatPriceInput(4.99)).toBe('4,99');
    expect(formatPriceInput(24.9)).toBe('24,90');
  });
});
