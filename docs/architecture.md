#  Service decomposition rationale

- Link service: Manages all links
- User service: Manages all users
- Redirect service: handles `GET /{shortUrl}` redirects with high throughput
- Stats service: collects and serves click statistics
- API Gateway: Frontend that connects all services

# Communication patterns

Synchronous tasks are sequential, blocking each other until they are complete at a predeterminate order while asynchronous tasks execute independently of one another. Asynchronous tasks are better suited for any system requiring real time interaction such as web servers, such as Shortly.

# Data ownership

The link service owns all registered links. The user service owns all the users. The stats service owns all statistics.

# Scalability considerations

Redirect service could be scaled to handle heavy traffic.

# Failure modes

If the redirect service fails, caching could be used to mitigate.
If the database fails, it could be replaced by it's slave (I didn't invent the term) if one is set up.
The API Gateway should stop routing traffic to unhealthy instances.

# Technology stack proposal

- Dotnet: the only framework i know how to use
- Redis: for caching
- PostgreSQL: offers better scalability than SQlite and is better at handling large data
- RabbitMQ: reliable message broker that is open source