# SLAK-U-Track Web

SLAK-U-Track Web is a .NET 10 and React application for class scheduling and student attendance reporting.

## Features

- Generate course attendance reports by professor, course, and class date.
- Download report data as CSV or use the browser's print dialog.
- Schedule recurring classes for selected weekdays between two dates.
- Store instructors, courses, classes, students, registrations, locations, and attendance in SQLite.
- View the SQLite file location in Settings.

On first run, a new SQLite database is created with a small, clearly labeled demo dataset. By default, the database is stored under the current user's application-data directory in `SLAK-U-Track-Web/attendance.db`. Set `SLAKUTRACK_DB_PATH` to choose a different SQLite file.

## Run in development

Start the API from the project root:

```sh
dotnet run
```

In another terminal, start the React development server:

```sh
cd client
npm install
npm run dev
```

Open the URL printed by Vite. The development server proxies `/api` requests to the .NET app at `http://localhost:5250`.

## Build and run the combined app

```sh
cd client
npm install
npm run build
cd ..
dotnet run
```

The .NET app serves the built React client and API from the same origin at `http://localhost:5250`.

## Run tests

Run the .NET API integration tests and React utility tests with:

```sh
dotnet test SlakUTrack.sln
cd client
npm test
```

## GitHub Actions and releases

The `Build, test, and release` workflow runs the frontend tests, lint, production build, and .NET test suite on pushes to any branch, pull requests, and manual dispatches. Pushing a version tag such as `v1.0.0` runs those checks, packages the published app, and creates a GitHub Release with generated release notes and a downloadable ZIP.

The release ZIP is framework-dependent and platform-neutral; it requires the .NET 10 ASP.NET Core Runtime. Unzip it and run `dotnet SlakUTrack.WebApplication.dll`.
