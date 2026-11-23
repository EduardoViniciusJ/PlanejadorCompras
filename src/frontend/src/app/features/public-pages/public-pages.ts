/**
 * Índice das páginas públicas de busca (segmentos e guias).
 *
 * Fica fora dos componentes para ser leve: as rotas, o pré-render, o rodapé e os links
 * entre páginas leem daqui. O texto de cada página fica nos arquivos `*.content.ts`,
 * carregados só junto com a página.
 *
 * Para criar uma página nova: acrescente a entrada aqui e o conteúdo no arquivo do tipo.
 * O sitemap é gerado no build a partir das páginas pré-renderizadas.
 */

export type SegmentPageSlug =
  'para-restaurantes' | 'para-mercados' | 'para-lojas' | 'para-clinicas';

export type GuidePageSlug =
  | 'o-que-e-mapa-de-cotacao'
  | 'como-fazer-equalizacao-de-propostas'
  | 'planilha-de-cotacao-de-compras';

export interface PublicPageEntry<TSlug extends string = string> {
  /** Caminho sem a barra inicial; também é a chave do conteúdo. */
  readonly slug: TSlug;
  /** Texto curto para links e para a trilha (breadcrumb). */
  readonly label: string;
  /** Título da aba e do resultado de busca. */
  readonly title: string;
  /** Descrição do resultado de busca e da prévia de link. */
  readonly description: string;
}

/** Dados que cada rota de página de busca entrega ao componente e ao `SeoService`. */
export interface PublicPageRouteData {
  readonly slug: string;
  readonly description: string;
}

export const SEGMENT_PAGES: readonly PublicPageEntry<SegmentPageSlug>[] = [
  {
    slug: 'para-restaurantes',
    label: 'Restaurantes',
    title: 'Cotação de compras para restaurantes | Planejador de Compras',
    description:
      'Cote hortifrúti, carnes, laticínios e descartáveis com vários fornecedores, compare no mapa de preços e emita o pedido. Gratuito e de código aberto.',
  },
  {
    slug: 'para-mercados',
    label: 'Mercados',
    title: 'Cotação de compras para mercados e mercearias | Planejador de Compras',
    description:
      'Transforme a lista de reposição em cotação em PDF, compare distribuidores item por item e feche o pedido com quem cobrou menos. Sistema gratuito para mercados.',
  },
  {
    slug: 'para-lojas',
    label: 'Lojas',
    title: 'Cotação de compras para lojas e varejo | Planejador de Compras',
    description:
      'Mercadoria para revenda e material de uso cotados no mesmo lugar: solicitação em PDF, mapa de preços entre fornecedores e pedido de compra. Gratuito para lojas.',
  },
  {
    slug: 'para-clinicas',
    label: 'Clínicas',
    title: 'Cotação de materiais para clínicas e consultórios | Planejador de Compras',
    description:
      'Cote luvas, descartáveis e materiais de consumo com vários distribuidores, compare no mapa de preços e registre cada pedido de compra. Gratuito para clínicas.',
  },
];

export const GUIDE_PAGES: readonly PublicPageEntry<GuidePageSlug>[] = [
  {
    slug: 'o-que-e-mapa-de-cotacao',
    label: 'O que é mapa de cotação',
    title: 'O que é mapa de cotação (mapa de preços) e como montar | Planejador de Compras',
    description:
      'Mapa de cotação é a tabela que põe as propostas dos fornecedores lado a lado, item por item, para achar o menor preço e decidir a compra. Veja o que ele tem e como montar.',
  },
  {
    slug: 'como-fazer-equalizacao-de-propostas',
    label: 'Como fazer equalização de propostas',
    title: 'Como fazer equalização de propostas de fornecedores | Planejador de Compras',
    description:
      'Equalizar propostas é colocar as ofertas na mesma base (itens, unidade, frete, prazo e pagamento) antes de comparar. Passo a passo com exemplo.',
  },
  {
    slug: 'planilha-de-cotacao-de-compras',
    label: 'Planilha de cotação de compras',
    title: 'Planilha de cotação de compras: modelo e como montar | Planejador de Compras',
    description:
      'Como montar uma planilha de cotação de compras para comparar fornecedores, com modelo em CSV para baixar, e os sinais de que a planilha já não dá conta.',
  },
];

/** Todas as páginas de busca, na ordem em que aparecem nos links. */
export const PUBLIC_CONTENT_PAGES: readonly PublicPageEntry[] = [...SEGMENT_PAGES, ...GUIDE_PAGES];
