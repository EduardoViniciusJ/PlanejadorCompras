import { Question } from './public-page.model';

export interface BreadcrumbLink {
  readonly name: string;
  /** Endereço absoluto da página. */
  readonly url: string;
}

/** `BreadcrumbList` do schema.org, na ordem da trilha mostrada na página. */
export function breadcrumbList(links: readonly BreadcrumbLink[]): object {
  return {
    '@type': 'BreadcrumbList',
    itemListElement: links.map((link, index) => ({
      '@type': 'ListItem',
      position: index + 1,
      name: link.name,
      item: link.url,
    })),
  };
}

/** `FAQPage` com as mesmas perguntas e respostas mostradas na página. */
export function faqPage(questions: readonly Question[]): object {
  return {
    '@type': 'FAQPage',
    mainEntity: questions.map((item) => ({
      '@type': 'Question',
      name: item.question,
      acceptedAnswer: { '@type': 'Answer', text: item.answer },
    })),
  };
}

/** O produto, citado como assunto das páginas. Gratuito e de código aberto. */
export const SOFTWARE_APPLICATION = {
  '@type': 'SoftwareApplication',
  name: 'Planejador de Compras',
  applicationCategory: 'BusinessApplication',
  operatingSystem: 'Web',
  inLanguage: 'pt-BR',
  isAccessibleForFree: true,
} as const;
