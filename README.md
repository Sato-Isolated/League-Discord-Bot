# League-Discord-Bot
A SlashCommand Bot Discord to show the mmr of the game or of a player using riot api and whatismymmr api

The code is done a bit on the fly if you want to improve it or add functions you can make a PR

if you have an idea for a function make an issue and I will see what I can do

When you do a long function like "/checkmmr name mode" it will tell you that the application is not responding on discord
but it will send the command a few seconds later 

sometimes I get this error for the /checkmmr I don't know why :shrug: 

"System.Net.Http.HttpRequestException: Response status code does not indicate success: 404 (Not Found)."

(Aram MMR from https://euw.whatismymmr.com/)

![image](https://user-images.githubusercontent.com/12450341/191113697-4d5482e6-fcb7-480a-95d5-61f09041af49.png)

(Ranked Stats)

![image](https://user-images.githubusercontent.com/12450341/191114426-d8630038-b271-49e6-b210-a3d959161f07.png)

(If MMR is available)

![image](https://user-images.githubusercontent.com/12450341/191180433-d15e379b-f8e7-461f-8a0e-8f429d28aaf1.png)
 

for the bot to work you must put your token for the bot discord in program.cs 

your Server ID in InteractionHandler.cs

and your riot api key in mmrmodule.cs
