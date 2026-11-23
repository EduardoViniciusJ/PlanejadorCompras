import { GuidePageSlug } from '../public-pages';
import { GuidePageContent } from '../models/public-page.model';

/**
 * Texto dos guias. O primeiro parágrafo de cada um responde à pergunta do título de forma
 * direta, para quem chega pela busca e para assistentes de IA que citam a página.
 * Os valores das tabelas são ilustrativos.
 */
export const GUIDE_PAGE_CONTENT: Record<GuidePageSlug, GuidePageContent> = {
  'o-que-e-mapa-de-cotacao': {
    slug: 'o-que-e-mapa-de-cotacao',
    eyebrow: 'Guia de compras',
    headline: 'O que é mapa de cotação (mapa de preços)',
    answer:
      'Mapa de cotação, também chamado de mapa de preços ou mapa comparativo de preços, é uma tabela que coloca lado a lado os preços que cada fornecedor ofereceu para os mesmos itens de uma compra. Cada linha é um item, cada coluna é um fornecedor, e o menor preço de cada linha fica em destaque. Ele serve para decidir de quem comprar e para registrar por que aquela proposta foi escolhida.',
    sections: [
      {
        id: 'o-que-tem',
        title: 'O que vai em um mapa de cotação',
        bullets: [
          'Item e especificação: o que está sendo comprado, com marca, tamanho ou embalagem quando isso muda o preço.',
          'Unidade e quantidade: quilo, caixa, unidade, e quanto vai ser comprado.',
          'Preço unitário de cada fornecedor, na mesma unidade para todos.',
          'Total por item e total por fornecedor, para ver a compra inteira.',
          'Condições que mudam o custo: frete, prazo de entrega, forma de pagamento e validade da proposta.',
          'O vencedor de cada item, ou da compra toda, e a data da decisão.',
        ],
      },
      {
        id: 'exemplo',
        title: 'Exemplo de mapa de cotação',
        paragraphs: [
          'Três fornecedores responderam à mesma solicitação com três itens. Os valores são ilustrativos.',
        ],
        table: {
          caption: 'Mapa de cotação com três fornecedores (valores ilustrativos)',
          columns: ['Item', 'Quantidade', 'Fornecedor A', 'Fornecedor B', 'Fornecedor C'],
          rows: [
            ['Produto 1', '10 cx', 'R$ 45,00', 'R$ 42,50 (menor)', 'R$ 44,00'],
            ['Produto 2', '50 un', 'R$ 3,20', 'R$ 3,40', 'R$ 3,10 (menor)'],
            ['Produto 3', '20 kg', 'R$ 12,00', 'R$ 11,80 (menor)', 'R$ 12,60'],
            ['Total do fornecedor', '', 'R$ 850,00', 'R$ 831,00', 'R$ 847,00'],
          ],
        },
        after: [
          'Lendo o mapa: o Fornecedor B tem o menor total, mas o Fornecedor C cobra menos no Produto 2. Comprando cada item de quem ofereceu o menor preço, a compra sairia por R$ 816,00, desde que dividir o pedido não traga frete ou pedido mínimo que anule a diferença.',
        ],
      },
      {
        id: 'como-montar',
        title: 'Como montar um mapa de cotação',
        ordered: true,
        bullets: [
          'Feche a lista antes de pedir preço: itens, especificação, unidade e quantidade.',
          'Envie a mesma solicitação a todos os fornecedores, com os itens na mesma ordem e um prazo para resposta.',
          'Lance cada resposta em uma coluna, convertendo para a unidade da lista quando o fornecedor cotar outra embalagem.',
          'Marque o menor preço de cada item e some o total de cada fornecedor.',
          'Anote frete, prazo e pagamento de cada proposta: é aqui que a comparação vira equalização.',
          'Registre a decisão e guarde o mapa junto do pedido de compra.',
        ],
      },
      {
        id: 'diferenca',
        title: 'Mapa de cotação, mapa de preços e equalização',
        paragraphs: [
          'Mapa de cotação e mapa de preços são nomes diferentes para a mesma tabela. Em empresas e órgãos públicos também aparece como mapa comparativo de preços.',
          'Equalização é o passo que deixa o mapa justo: ajustar as propostas para a mesma base de itens, unidades e condições antes de comparar os totais.',
        ],
      },
      {
        id: 'sem-planilha',
        title: 'Mapa de cotação sem montar planilha',
        paragraphs: [
          'No Planejador de Compras, o mapa de preços é montado a partir da lista de compras: cada fornecedor vira uma coluna, o menor preço de cada item aparece em destaque e o total de cada fornecedor é calculado sozinho. Dali você salva a equalização, exporta para Excel ou PDF e emite o pedido de compra.',
        ],
      },
    ],
    questions: [
      {
        question: 'Mapa de cotação e mapa de preços são a mesma coisa?',
        answer:
          'Sim. Os dois nomes se referem à tabela que compara, item por item, os preços de fornecedores diferentes para a mesma compra.',
      },
      {
        question: 'Quantos fornecedores devo cotar?',
        answer:
          'Para empresa privada não existe número obrigatório. O que importa é que as propostas sejam comparáveis: mesmos itens, mesma unidade e condições conhecidas.',
      },
      {
        question: 'O menor preço sempre vence?',
        answer:
          'Não necessariamente. Frete, prazo de entrega, forma de pagamento e itens que faltam na proposta mudam o custo final. Por isso o mapa costuma vir acompanhado da equalização.',
      },
    ],
    closingTitle: 'Monte o mapa de preços a partir da lista',
    closingText:
      'Crie a conta, cadastre a lista e os fornecedores e veja o menor preço de cada item em destaque. Gratuito e de código aberto.',
    updatedAt: '2026-11-23',
  },

  'como-fazer-equalizacao-de-propostas': {
    slug: 'como-fazer-equalizacao-de-propostas',
    eyebrow: 'Guia de compras',
    headline: 'Como fazer equalização de propostas',
    answer:
      'Equalização de propostas é colocar as ofertas de fornecedores diferentes na mesma base antes de comparar: mesmos itens, mesma unidade e quantidade, e as mesmas condições de frete, prazo e pagamento. Para fazer, ajuste cada proposta a essa base comum, some o que o fornecedor cobra à parte e só então compare os totais. Sem equalizar, o preço mais baixo da tabela pode não ser a compra mais barata.',
    sections: [
      {
        id: 'por-que',
        title: 'Por que as propostas não chegam comparáveis',
        bullets: [
          'Um fornecedor deixa de cotar algum item da lista.',
          'A embalagem é outra: caixa com 12 em vez de unidade, fardo em vez de pacote.',
          'O frete está incluso em uma proposta e cobrado à parte em outra.',
          'O prazo de pagamento muda: à vista, 14 dias, 28 dias.',
          'A marca ou a especificação oferecida não é a que foi pedida.',
          'A validade da proposta vence antes de a compra ser fechada.',
        ],
      },
      {
        id: 'passo-a-passo',
        title: 'Passo a passo da equalização',
        ordered: true,
        bullets: [
          'Defina a base: a lista com especificação, unidade e quantidade de cada item.',
          'Confira a cobertura: marque os itens que cada fornecedor deixou de cotar.',
          'Converta para a mesma unidade. Exemplo: caixa com 12 a R$ 30,00 equivale a R$ 2,50 a unidade.',
          'Some frete, taxas e qualquer custo que o fornecedor cobre à parte.',
          'Anote prazo de entrega, condição de pagamento e validade de cada proposta.',
          'Compare o total equalizado da compra inteira e também item por item.',
          'Registre a decisão com a data e o motivo da escolha.',
        ],
      },
      {
        id: 'exemplo',
        title: 'Exemplo de equalização',
        paragraphs: [
          'Três propostas para a mesma lista de cinco itens. Os valores são ilustrativos.',
        ],
        table: {
          caption: 'Propostas antes e depois de equalizar (valores ilustrativos)',
          columns: ['', 'Fornecedor A', 'Fornecedor B', 'Fornecedor C'],
          rows: [
            ['Itens cotados', '5 de 5', '5 de 5', '4 de 5'],
            ['Soma dos itens', 'R$ 1.000,00', 'R$ 960,00', 'R$ 900,00'],
            ['Frete', 'Incluso', 'R$ 60,00', 'R$ 40,00'],
            ['Total equalizado', 'R$ 1.000,00', 'R$ 1.020,00', 'Incompleto'],
            ['Pagamento', '28 dias', 'À vista', '14 dias'],
          ],
        },
        after: [
          'Pela soma dos itens, o Fornecedor C parece o mais barato, mas falta um item na proposta dele. Entre os completos, o Fornecedor B tem a menor soma, mas com o frete fica mais caro que o Fornecedor A, que ainda dá 28 dias para pagar.',
        ],
      },
      {
        id: 'dividir',
        title: 'Comprar tudo de um fornecedor ou dividir',
        paragraphs: [
          'Depois de equalizar, há duas leituras. A melhor combinação item a item compra cada produto de quem cobrou menos. O melhor fornecedor único é o de menor total entre os que cotaram todos os itens.',
          'Dividir costuma baixar o preço dos itens, mas pode trazer mais de um frete, pedido mínimo por fornecedor e mais entregas para conferir. Compare as duas leituras antes de fechar.',
        ],
      },
      {
        id: 'no-sistema',
        title: 'Equalização no Planejador de Compras',
        paragraphs: [
          'O mapa de preços mostra o menor preço de cada item, o total de cada fornecedor e a melhor combinação. Quando você salva a equalização, ela fica guardada como foi decidida, mesmo que a lista mude depois, e o pedido de compra é emitido a partir dela, com a condição de pagamento combinada.',
        ],
      },
    ],
    questions: [
      {
        question: 'Qual a diferença entre equalização e mapa de cotação?',
        answer:
          'O mapa de cotação é a tabela com os preços lado a lado. A equalização é o ajuste das propostas para a mesma base, para que a comparação nessa tabela seja justa.',
      },
      {
        question: 'O que fazer quando um fornecedor não cota todos os itens?',
        answer:
          'Marque a proposta como incompleta. Ela pode vencer nos itens que cotou, mas não deve ser comparada pelo total com quem cotou a lista inteira.',
      },
      {
        question: 'Preciso equalizar compras pequenas?',
        answer:
          'Quando as propostas já chegam na mesma unidade e com as mesmas condições, a equalização é rápida. Ela pesa mais quando há frete, prazos ou embalagens diferentes.',
      },
    ],
    closingTitle: 'Salve a equalização junto do pedido',
    closingText:
      'No Planejador de Compras a comparação, a decisão e o pedido ficam no mesmo lugar. Crie a conta gratuita e teste com a sua próxima compra.',
    updatedAt: '2026-11-23',
  },

  'planilha-de-cotacao-de-compras': {
    slug: 'planilha-de-cotacao-de-compras',
    eyebrow: 'Modelo para baixar',
    headline: 'Planilha de cotação de compras: como montar e quando trocar',
    answer:
      'Uma planilha de cotação de compras tem uma linha por item e, para cada fornecedor, uma coluna de preço unitário e outra de total. No fim de cada linha, uma coluna aponta o menor preço; embaixo, uma linha soma o total de cada fornecedor e outras registram frete, prazo, pagamento e validade. Abaixo há um modelo pronto em CSV, que abre no Excel, no Google Planilhas e no LibreOffice.',
    download: {
      href: '/modelos/planilha-de-cotacao-de-compras.csv',
      fileName: 'planilha-de-cotacao-de-compras.csv',
      label: 'Baixar modelo de planilha (.csv)',
      text: 'Colunas para três fornecedores, menor preço e fornecedor escolhido, além das linhas de frete, prazo de entrega, pagamento e validade. O arquivo vem sem fórmulas; as sugestões estão logo abaixo.',
    },
    sections: [
      {
        id: 'colunas',
        title: 'Colunas da planilha',
        table: {
          caption: 'Colunas do modelo, na ordem do arquivo',
          columns: ['Coluna', 'Nome', 'Para que serve'],
          rows: [
            ['A', 'Item', 'Nome do produto ou serviço.'],
            ['B', 'Especificação', 'Marca, tamanho ou embalagem, quando muda o preço.'],
            ['C', 'Unidade', 'kg, caixa, unidade, pacote.'],
            ['D', 'Quantidade', 'Quanto vai ser comprado.'],
            ['E, G, I', 'Preço unitário', 'O preço de cada fornecedor, na unidade da coluna C.'],
            ['F, H, J', 'Total', 'Quantidade vezes preço unitário.'],
            ['K', 'Menor preço unitário', 'O menor entre os três fornecedores.'],
            ['L', 'Fornecedor escolhido', 'Quem venceu aquele item.'],
            ['M', 'Observações', 'Item que faltou, marca diferente, prazo especial.'],
          ],
        },
      },
      {
        id: 'formulas',
        title: 'Fórmulas para acrescentar',
        paragraphs: [
          'Depois de abrir o arquivo, salve no formato do seu programa (.xlsx, .ods ou Google Planilhas) para guardar as fórmulas. Os exemplos usam a linha 2 e o Excel em português; no Google Planilhas em inglês, troque MÍNIMO por MIN e SOMA por SUM.',
        ],
        bullets: [
          'Total do Fornecedor 1 (coluna F): =D2*E2. Repita nas colunas H (=D2*G2) e J (=D2*I2).',
          'Menor preço unitário (coluna K): =MÍNIMO(E2;G2;I2).',
          'Total de cada fornecedor, na linha “Total do fornecedor”: =SOMA(F2:F31), ajustando o intervalo ao número de itens.',
          'Destaque do menor preço: formatação condicional nas colunas E, G e I, quando a célula for igual à coluna K.',
        ],
      },
      {
        id: 'como-usar',
        title: 'Como usar na rotina',
        ordered: true,
        bullets: [
          'Faça uma cópia do modelo para cada cotação e dê um nome com a data.',
          'Preencha item, especificação, unidade e quantidade antes de pedir preço.',
          'Envie a mesma lista a todos os fornecedores.',
          'Lance as respostas na unidade da coluna C, convertendo quando vier outra embalagem.',
          'Some frete e anote prazo e pagamento nas linhas de baixo antes de escolher.',
          'Marque o fornecedor escolhido e guarde a planilha junto do pedido.',
        ],
      },
      {
        id: 'limites',
        title: 'Quando a planilha deixa de dar conta',
        paragraphs: [
          'A planilha funciona bem para poucas compras. Os sinais de que ela ficou pequena:',
        ],
        bullets: [
          'Cada fornecedor novo exige mais duas colunas e o ajuste de todas as fórmulas.',
          'Alguém insere uma coluna, e o menor preço passa a olhar para a célula errada.',
          'Os preços chegam por WhatsApp, foto e PDF e precisam ser redigitados um a um.',
          'O pedido de compra é montado à parte, copiando valores da planilha.',
          'Para saber quanto foi pago em uma compra antiga, é preciso procurar entre várias cópias do arquivo.',
        ],
      },
      {
        id: 'alternativa',
        title: 'Uma alternativa à planilha',
        paragraphs: [
          'O Planejador de Compras faz o mesmo caminho sem fórmula para manter: você monta a lista (pode colar as linhas desta planilha), envia a solicitação de cotação em PDF pelo WhatsApp, compara os fornecedores no mapa de preços com o menor preço em destaque e emite o pedido de compra em PDF. Quando precisar de planilha, o mapa sai em Excel.',
        ],
      },
    ],
    questions: [
      {
        question: 'O modelo abre no Google Planilhas?',
        answer:
          'Abre. No Google Planilhas, use Arquivo > Importar e escolha o arquivo baixado. No Excel e no LibreOffice, basta abrir o arquivo.',
      },
      {
        question: 'Por que o modelo é CSV e não .xlsx?',
        answer:
          'CSV abre em qualquer programa de planilha. Ele não guarda fórmulas nem cores, então salve no formato do seu programa depois de abrir.',
      },
      {
        question: 'Posso levar esta planilha para o Planejador de Compras?',
        answer:
          'Pode. Copie as linhas de item, quantidade e unidade e cole na lista de compras: elas entram de uma vez.',
      },
    ],
    closingTitle: 'Troque as fórmulas por um mapa de preços pronto',
    closingText:
      'Crie a conta, cole a sua planilha na primeira lista e compare os fornecedores sem montar fórmula. Gratuito e de código aberto.',
    updatedAt: '2026-11-23',
  },
};
