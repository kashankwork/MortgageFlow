# Screenshots

This folder contains public-safe screenshots for reviewing the finished MortgageFlow MVP. All screenshots must use synthetic data only and should show app content without personal browser chrome, email, desktop notifications, tokens, or secrets.

## Included images

| File | What it proves |
| --- | --- |
| `01-login.png` | Synthetic role login, in-memory auth flow, and public-safe demo copy. |
| `02-loans.png` | Role-aware loan list with stable workflow data. |
| `03-loan-detail.png` | Loan detail, status, priority, borrower/property summary, workflow actions, and timeline area. |
| `04-team-queue.png` | Team Lead queue visibility and assignment-oriented review. |
| `05-my-queue.png` | Processor or Underwriter personal queue view. |
| `06-openapi.png` | API contract is available for local review. |
| `07-ci.png` | GitHub Actions backend, frontend, and Docker quality gates pass. |

## Recapture guidance

1. Start from a clean local database:

   ```bash
   docker compose down -v
   docker compose up -d --build
   ```

2. Open `http://localhost:5173`.
3. Use synthetic accounts from the README and the private `Seed__DemoPassword` value from `.env`.
4. Capture only the app viewport or GitHub Actions content.
5. Crop out personal browser tabs, profile details, local terminal secrets, and desktop notifications.
