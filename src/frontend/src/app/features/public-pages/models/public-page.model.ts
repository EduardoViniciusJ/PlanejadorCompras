import { GuidePageSlug, SegmentPageSlug } from '../public-pages';

export interface TitledText {
  readonly title: string;
  readonly text: string;
}

export interface Question {
  readonly question: string;
  readonly answer: string;
}

/** Linha do mapa de preços de exemplo: preços na ordem dos fornecedores. */
export interface ExampleRow {
  readonly item: string;
  readonly quantity: number;
  readonly unit: string;
  readonly prices: readonly number[];
}

/** Mapa de preços ilustrativo. Menor preço e totais são calculados, não digitados. */
export interface PriceMapExample {
  readonly listName: string;
  readonly suppliers: readonly string[];
  readonly rows: readonly ExampleRow[];
}

export interface SegmentPageContent {
  readonly slug: SegmentPageSlug;
  /** Linha curta acima do título. */
  readonly eyebrow: string;
  readonly headline: string;
  /** Primeiro parágrafo: diz em uma frase o que o produto faz para esse público. */
  readonly lead: string;
  readonly painsTitle: string;
  readonly pains: readonly TitledText[];
  readonly exampleTitle: string;
  readonly exampleText: string;
  readonly example: PriceMapExample;
  readonly flowTitle: string;
  /** Lista, cotação, mapa de preços e pedido, nessa ordem, com exemplos do segmento. */
  readonly flow: readonly [TitledText, TitledText, TitledText, TitledText];
  readonly questions: readonly Question[];
  readonly closingTitle: string;
  readonly closingText: string;
}

export interface GuideTable {
  readonly caption: string;
  readonly columns: readonly string[];
  readonly rows: readonly (readonly string[])[];
}

export interface GuideSection {
  readonly id: string;
  readonly title: string;
  readonly paragraphs?: readonly string[];
  readonly bullets?: readonly string[];
  /** Quando true, os itens de `bullets` são passos numerados. */
  readonly ordered?: boolean;
  readonly table?: GuideTable;
  /** Parágrafos mostrados depois da lista ou da tabela. */
  readonly after?: readonly string[];
}

export interface GuideDownload {
  readonly href: string;
  readonly fileName: string;
  readonly label: string;
  readonly text: string;
}

export interface GuidePageContent {
  readonly slug: GuidePageSlug;
  readonly eyebrow: string;
  readonly headline: string;
  /** Resposta direta à pergunta do título, no primeiro parágrafo. */
  readonly answer: string;
  readonly download?: GuideDownload;
  readonly sections: readonly GuideSection[];
  readonly questions: readonly Question[];
  readonly closingTitle: string;
  readonly closingText: string;
  /** Data da última revisão do texto, em ISO (AAAA-MM-DD). */
  readonly updatedAt: string;
}
