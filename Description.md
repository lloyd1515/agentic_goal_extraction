Note: This task is vague on purpose and it needs to be implemented individually (do not discuss your findings with your colleagues), as we want to let everyone explore and see what their approach is. There is no single answer or correct answer.

A text area input should be provided:
User enters the summary/transcript of the discussion between the manager and the employee
Based on the summary/transcript, an AI agent will extract the goals and display them to the user
User is able to edit the goal list and then proceed to saving it

Some technical direction:
We will need to implement a few pieces invoked from our command handler
A tool which can query the database is needed to check for our relevant data
An LLM interaction is needed: agent will invoke the tool, add the "summary/transcript" and send it to the LLM to get a structured output with the extracted goals
You can use Titan url and Qwen model with your API key (it's OpenAI compliant)
The goals are then presented to the user
On FE the user can still edit the goals (AI won't have the capability to write to the database)
When everything is filled in properly, the user saves the goal to the database (an additional command handler which saves multiple goals may be needed)


Sample workflow:
 1. **Browser / Client App**
   - The user enters a **summary or transcript of the manager–employee discussion**.
   - The browser sends this information to the backend through the **API / Controller**.
   - After the AI processes it, the browser receives the extracted goals and displays them to the user.
   - The user can **review and modify the goals** before saving.
2. **API / Controller → CommandHandler**
   - The API/controller passes the request to a **CommandHandler**.
   - The CommandHandler coordinates the AI-related operation rather than directly handling the LLM interaction itself.
3. **CommandHandler → AI Agent**
   - The CommandHandler invokes the **AI Agent**.
   - The agent receives the discussion summary/transcript as context.
4. **AI Agent → Tools / LLM**
   - The AI Agent can invoke a **database/query tool** to retrieve relevant existing data needed for the extraction.
   - It also sends the transcript/summary to the **OpenAI-compatible LLM** (e.g. Qwen through the specified Titan URL).
   - The LLM returns a **structured output containing the extracted goals**.
5. **AI Agent → CommandHandler**
   - The structured goals are returned from the AI Agent to the CommandHandler.
   - Importantly, the AI flow **does not write the goals directly to the database**.
6. **CommandHandler → Browser**
   - The extracted goals are returned to the frontend.
   - The user reviews and edits them as necessary.
7. **Saving**
   - Once the user is satisfied with the goals, the frontend sends the finalized goals back to the backend.
   - A separate **save command handler** can then persist the multiple goals to the database.

 In short:

 **User enters transcript → API/CommandHandler → AI Agent → database tool + LLM → structured goals → CommandHandler → UI → user edits → save command → database.**

 The key separation in the diagram is that **AI is responsible for extracting/suggesting goals, while the user and a separate backend save operation are responsible for the final database write.**