# Vending Machine API

.NET 8 Web API with SQLite (EF Core).

## Running

Requires the .NET 8 SDK.

```bash
dotnet run --project src/Requests.Api
```

Open http://localhost:5080/swagger. The database file (`vending.db`) is created on startup and migrations run
automatically; categories, product types and users are seeded.

1. `POST /auth/login` with `{ "username": "admin" }` (or `"user"`), copy `accessToken`.
2. Click **Authorize**, paste the token.
3. Call the endpoints.

### Running tests

```bash
dotnet test
```

## Assumptions

- A machine has a name, a location (region, zip code of 5–10 digits, optional description) and a maximum number
  of shelves (1–20, default 20, limit set by `MAX_SHELVES_LIMIT`).
- A machine is identified by a server-generated GUID. Names are not unique.
- A shelf holds one product type, set when the shelf is added. Price is stored on the shelf, so the same product
  can cost differently in different machines. Shelf capacity is at most `MAX_SHELF_CAPACITY` (50).
- Product categories and product types are an editable list (admin only); names are unique.
- Loading inventory must match the shelf's product type and must not exceed its capacity.
- An empty shelf keeps its product type.
- A purchase is one item from one shelf. There is no payment and no purchase history (purchases are logged only).
- Roles: `admin` manages machines, shelves, products and inventory; `user` and `admin` can view a machine and buy.
- Login is by username only, with no password. This is a test login.
- Stock checks run in a transaction, so concurrent loads can't overfill a shelf and concurrent purchases can't
  sell the last item twice.

## Questions I would ask

- What data do we expect for each machine? How is it identified? Can a machine move from place to place?
- Does a machine have a single unique name or not?
- Do we need an accessible unique identifier?
- Is there a maximum number of shelves? Is it a fixed number or a variable one?
- Do we need to store a product category (snack, drink, etc.)?
- Are there limits on what a machine can hold and how much (how many drinks, water required, etc.)?
- Who can see the machines' details?
- Are there different machines for different types (food, drinks...)?
- Is a different size a different product, or the same product with a size and price?
- Is there a fixed price per product or a price per machine (store the price on the shelf or on the product)?
- Are product types a fixed list or an editable one?
- Does an empty shelf keep its last product, or reset productId to null when quantity reaches 0 (matters once selling exists)?
- Who can buy? A registered user or everyone?
- Can someone buy several items or a larger quantity?
- Do we need a purchase history (if the user is registered, it basically makes sense)?
- What is meant by a machine's location? What details do we want to know?
