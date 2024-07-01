# League-Discord-Bot

## About

League-Discord-Bot is a SlashCommand Bot for Discord that provides detailed information about League of Legends games. It utilizes the [Camille API](https://github.com/MingweiSamuel/Camille) and [Discord.NET](https://github.com/discord-net/Discord.Net).

The project is a work-in-progress. Contributions and improvements are welcome! Feel free to submit a pull request (PR) or create an issue with any ideas or feature requests.

## Features

### Ranked Stats

- **Command:** `/rank username`
  
  Displays the ranked stats of a specified user.

  ![Ranked Stats](https://user-images.githubusercontent.com/12450341/191114426-d8630038-b271-49e6-b210-a3d959161f07.png)

### Stalk Game

- **Command:** `/stalk username`
  
  Provides details about the current game of a specified user.

  ![Stalk Game](https://user-images.githubusercontent.com/12450341/214114945-eb72810d-cfc6-43c4-966e-feee7b99d9c0.png)

### Make Excel

- **Command:** `/makeexcel (username) BIPBIP (tagline) euw or 12453`
  
  Generates an Excel file with game details. The tagline is the part after the `#` in your username.

  ![Excel Command](https://github.com/Sato-Isolated/League-Discord-Bot/assets/12450341/ab1fe721-1284-4d91-88b3-3fa3174c3d96)
  ![Excel Example 1](https://github.com/Sato-Isolated/League-Discord-Bot/assets/12450341/ed17f87a-c2de-4cd7-b8c3-71744273aa30)
  ![Excel Example 2](https://github.com/Sato-Isolated/League-Discord-Bot/assets/12450341/f46eb601-fb8c-4609-adbd-355cacf64311)

## Setup

To set up the bot, you need to configure several keys and IDs in the code:

1. **Discord Bot Token:** Add your bot's token in [`Program.cs`](https://github.com/Sato-Isolated/League-Discord-Bot/blob/e8791864190a44c1f834a06cb900f42eeb0f52c5/Program.cs#L76).
2. **Server ID:** Insert your server ID in [`InteractionHandler.cs`](https://github.com/Sato-Isolated/League-Discord-Bot/blob/e8791864190a44c1f834a06cb900f42eeb0f52c5/InteractionHandler.cs#L43).
3. **Discord Webhook:** Set your webhook in [`LeagueMethod.cs`](https://github.com/Sato-Isolated/League-Discord-Bot/blob/e8791864190a44c1f834a06cb900f42eeb0f52c5/Modules/LeagueMethod.cs#L13).
4. **Riot API Key:** Place your Riot API key in [`LeagueCommand.cs`](https://github.com/Sato-Isolated/League-Discord-Bot/blob/e8791864190a44c1f834a06cb900f42eeb0f52c5/Modules/LeagueCommand.cs#L61).

## Contribution

If you want to contribute to this project, follow these steps:

1. Fork the repository.
2. Create a new branch (`git checkout -b feature-branch`).
3. Make your changes.
4. Commit your changes (`git commit -m 'Add some feature'`).
5. Push to the branch (`git push origin feature-branch`).
6. Open a pull request.
