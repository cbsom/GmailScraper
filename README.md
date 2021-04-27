# GmailScraper
Gets a list of emails from Gmail using the regular Gmail search then searches through those emails to find values using regular expressions.

It's not as complicated/convoluted as it sounds and it can be awfully useful.

Note: Before the project can be built, the file *credentials.json* must be placed into the project root.
To acquire this file: 
1. Create a Google API project following the instructions at https://developers.google.com/workspace/guides/create-project.
2. Enable the Gmail API in your project.
3. Create your credentials following the instructions at: https://developers.google.com/workspace/guides/create-credentials
4. Make sure you follow the instructions for getting credentials for a Desktop Application (https://developers.google.com/workspace/guides/create-credentials#desktop).
5. Take your credentials.json file and place it in the root of the GmailScraper application. If the project builds, you are good to go.
6. For your own good, DO NOT check the credentials.json file into your repository.

The first time the application runs, it will open the browser to get gmail permissions.
