import { ShoppingList } from '../../shopping-lists/models/shopping-list.model';

export type OnboardingStepId =
  | 'create-list'
  | 'add-supplier'
  | 'send-quotation-request'
  | 'enter-prices'
  | 'issue-purchase-order';

/** O que já existe nas telas que não são a de listas (vem da API, carregado uma vez). */
export interface OnboardingRemoteProgress {
  readonly hasSupplier: boolean;
  readonly hasQuotationRequest: boolean;
  readonly hasPurchaseOrder: boolean;
}

export type OnboardingStepAction =
  | { readonly kind: 'link'; readonly commands: readonly string[] }
  | { readonly kind: 'create-list' }
  | { readonly kind: 'request-quotation'; readonly list: ShoppingList };

export interface OnboardingStep {
  readonly id: OnboardingStepId;
  readonly title: string;
  readonly description: string;
  readonly done: boolean;
  readonly actionLabel: string;
  readonly action: OnboardingStepAction;
}

/**
 * Monta os cinco primeiros passos a partir dos dados reais do usuário.
 * Nenhum passo é marcado à mão: concluído significa que o registro existe.
 */
export function buildOnboardingSteps(
  lists: readonly ShoppingList[],
  remote: OnboardingRemoteProgress,
): readonly OnboardingStep[] {
  const firstList = lists[0] ?? null;
  const listWithItems = lists.find((list) => list.itemCount > 0) ?? null;
  const listWithPrices = lists.find((list) => list.quotedItemCount > 0) ?? null;

  return [
    {
      id: 'create-list',
      title: 'Crie sua primeira lista de compras',
      description: 'Junte os itens que você precisa comprar, com quantidade e unidade.',
      done: lists.length > 0,
      actionLabel: 'Criar lista',
      action: { kind: 'create-list' },
    },
    {
      id: 'add-supplier',
      title: 'Cadastre um fornecedor',
      description: 'Guarde nome, CNPJ e contato de quem costuma vender para você.',
      done: remote.hasSupplier,
      actionLabel: 'Cadastrar fornecedor',
      action: { kind: 'link', commands: ['/app/suppliers'] },
    },
    {
      id: 'send-quotation-request',
      title: 'Envie uma solicitação de cotação',
      description: 'Gere o PDF da lista e mande para os fornecedores pelo WhatsApp.',
      done: remote.hasQuotationRequest,
      ...quotationRequestAction(firstList, listWithItems),
    },
    {
      id: 'enter-prices',
      title: 'Lance os preços no mapa',
      description: 'Digite o preço que cada fornecedor passou para comparar lado a lado.',
      done: listWithPrices !== null,
      actionLabel: 'Abrir mapa de preços',
      action: {
        kind: 'link',
        commands: firstList ? ['/app/price-map', firstList.id] : ['/app/price-map'],
      },
    },
    {
      id: 'issue-purchase-order',
      title: 'Emita um pedido de compra',
      description: 'Na equalização, escolha o melhor fornecedor e gere o pedido em PDF.',
      done: remote.hasPurchaseOrder,
      actionLabel: 'Ir para a equalização',
      action: {
        kind: 'link',
        commands: listWithPrices
          ? ['/app/lists', listWithPrices.id, 'equalization']
          : ['/app/equalizations'],
      },
    },
  ];
}

function quotationRequestAction(
  firstList: ShoppingList | null,
  listWithItems: ShoppingList | null,
): Pick<OnboardingStep, 'actionLabel' | 'action'> {
  if (listWithItems) {
    return {
      actionLabel: 'Solicitar cotação',
      action: { kind: 'request-quotation', list: listWithItems },
    };
  }

  if (firstList) {
    return {
      actionLabel: 'Adicionar itens',
      action: { kind: 'link', commands: ['/app/price-map', firstList.id] },
    };
  }

  return { actionLabel: 'Criar lista primeiro', action: { kind: 'create-list' } };
}
