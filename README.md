# TaskZ - Ferramenta para gestão de projetos e tarefas

## Execução

Para executar o projeto, siga os passos abaixo:

1. Clone o repositório:
   ```bash
   git clone https://github.com/moisessantosf/TaskZ.git
   ```

2. Navegue até o diretório do projeto:
   ```bash
   cd TaskZ
   ```

3. Execute o projeto utilizando Docker Compose:
   ```bash
   docker-compose up -d --build
   ```

4. A API estará disponível em `http://localhost:5000`.

5. Swagger disponível em `http://localhost:5000/swagger`.

## Fase 2: Perguntas para Refinamento

   * Qual a carga (quantidade) de usuários logados e ao mesmo tempo deve ser garantida?
   * Qual a carga (quantidade) de projetos o sistema deve ser capaz de atender (diariamente, mensalmente e anualmente)?
   * No quesito velocidade de resposta (seja para cadastros, relatorios) existe algum parâmetro que deva ser cumprido?
   * O sistema será utilizado internamente ou será disponibilizado para diversos clientes, acessando via internet?
   * Existe algum plano para a implementação de autenticação e controle de acesso no sistema? 
   * Existe algum plano para disponibilizar esta API para acesso externo (integração com outros sistemas)?
   * Você deseja incluir notificações para os usuários (e-mail, push, etc.) ao criar ou atualizar tarefas?
   * Como a colaboração entre os membros da equipe deve funcionar? Existem níveis de permissões diferentes para cada usuário do projeto?
   * Há planos para permitir a customização de campos dos projetos e tarefas, como etiquetas ou campos adicionais?
   * Existe interesse em adicionar suporte a tarefas recorrentes?
   * O status hoje é pré definido, no futuro poderá ser permitido cadastrar novos status?
   * A prioridade hoje é pré definida, no futuro poderá ser permitido cadastrar novas prioridades?

## Fase 3: Possíveis Melhorias e Sugestões Futuras

   - Considerar o uso de uma arquitetura CQRS para separar comandos de leitura e escrita, facilitando manutenções e melhorando a performance em grandes volumes de dados.
   - Adotar Event Sourcing para manter um registro detalhado de todas as alterações no sistema, o que ajudaria em auditorias e no acompanhamento do histórico das tarefas.
   - Integrar com um provedor de autenticação, como OAuth2 ou OpenID Connect, para oferecer autenticação segura.
   - Implementar Rate Limiting para proteger a API contra ataques de negação de serviço (DoS).
   - Implementar Paginação e Filtragem para melhorar a usabilidade dos endpoints que retornam listas, como a listagem de projetos e tarefas.
   - Utilizar um provedor de nuvem, como AWS, Azure ou GCP, para escalabilidade e alta disponibilidade do sistema.
   - Implementar um sistema de cache distribuído como Redis para acelerar respostas a consultas frequentes.
   - Adicionar suporte para filas de mensagens, como RabbitMQ, Kafka ou Azure Service Bus, para tratar eventos que exigem reprocessamento ou execução assíncrona.
   - Adicionar observalidade para monitorar e analisar a telemetria do sistema, para isto poderia ser usado o OpenTelemetry, DataDog ou outra ferramenta.

