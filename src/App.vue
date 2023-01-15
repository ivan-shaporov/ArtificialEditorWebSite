<script setup lang="ts">
import { onMounted } from "vue";
import axios from "axios";

var lastDraft: string;
var lastId: string;
var lastPartition: string;

var draft: HTMLTextAreaElement;
var allowLog: HTMLInputElement;
var rewritten: HTMLDivElement;
var btnRewrite: HTMLButtonElement;
var btnReportProblem: HTMLButtonElement;
var defaultPlaceholder: string;
var defaultrewrite: string;

async function rewrite(): Promise<void> {
  if (draft.value.length == 0) {
    rewritten.innerText = defaultrewrite;
    return;
  }

  rewritten.innerText = "Let me think...";
  btnRewrite.disabled = true;
  const apiUrl = "api/RewriteEmail";
  let data = { text: draft.value, allowLog: allowLog.checked };
  lastDraft = data.text;

  axios
    .post(apiUrl, data)
    .then((response) => {
      rewritten.innerText = response.data.text;
      lastId = response.data.id;
      lastPartition = response.data.partition;
      btnReportProblem.disabled = false;
    })
    .catch(() => {
      rewritten.innerText = "Failed to process, please retry later.";
    })
    .finally(() => {
      btnRewrite.disabled = false;
    });
}

async function reportProblem() {
  if (!lastId) {
    return;
  }

  btnReportProblem.disabled = true;
  const apiUrl = "api/ReportProblem";
  let data = {
    partition: lastPartition,
    id: lastId,
    text: lastDraft,
    rewritten: rewritten.innerText,
  };

  fetch(apiUrl, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  })
    .then((response) => {
      if (!response.ok) {
        throw Error(response.statusText);
      }
    })
    .catch(() => {
      rewritten.innerText = "Failed to report the problem, please retry later."; // todo: find a better place for the message.
    })
    .finally(() => {
      btnReportProblem.disabled = false;
    });
}

onMounted(() => {
  draft = document.getElementById("draft") as HTMLTextAreaElement;
  allowLog = document.getElementById("allowLog") as HTMLInputElement;
  rewritten = document.getElementById("rewritten") as HTMLDivElement;
  btnRewrite = document.getElementById("btnRewrite") as HTMLButtonElement;
  btnReportProblem = document.getElementById(
    "btnReportProblem"
  ) as HTMLButtonElement;
  defaultPlaceholder = `John Doe,

I lost your invoice.
Send it again.
If you don't send it I cannot pay.

Jack`;

  defaultrewrite = `Dear Mr. Doe,

I hope this email finds you well. I seem to have misplaced your invoice and I was wondering if you could send it to me again. As I am unable to locate it, I am unable to process payment at this time.

I apologize for any inconvenience this may have caused and appreciate your prompt attention to this matter.

Sincerely,
Jack`;

  rewritten.innerText = defaultrewrite;
  draft.placeholder = defaultPlaceholder;
});
</script>

<template>
  <main>
    <div>
      I am Artificial Intelligence that can rewrite e-mails for you. Given the
      text below:
    </div>
    <div class="paper">
      <div class="paper-content">
        <textarea autofocus maxlength="500" rows="20" cols="100" id="draft" />
      </div>
    </div>

    <div style="padding-top: 1em">I would write it like this:</div>

    <div id="rewritten" class="paper-shadow"></div>

    <div>
      (allow reading by humans for improving the service
      <input id="allowLog" type="checkbox" />)
      <input type="button" value="Rewrite" id="btnRewrite" @click="rewrite" />
      <input
        type="button"
        value="Report problem"
        id="btnReportProblem"
        @click="reportProblem"
        disabled="true"
      />
    </div>
  </main>
</template>

<style scoped>
/** {
  font-family: Arial, Helvetica, sans-serif;
  font-size: 18px;
}

html,
body {
  margin: 0;
  border: 0;
  padding: 0;
  background-color: rgb(199, 208, 208);
}*/

main > h1 {
  font-size: 3.5em;
  margin-top: 20px;
}

/* https://codepen.io/MarcMalignan/pen/QbaXGg */
.paper {
  position: relative;
  width: 665px;
  height: 400px;
  background: #fafafa;
  border-radius: 10px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.3);
  overflow: hidden;
}
.paper:before {
  content: "";
  position: absolute;
  top: 0;
  bottom: 0;
  left: 0;
  width: 60px;
  background: radial-gradient(#575450 6px, transparent 7px) repeat-y;
  background-size: 30px 30px;
  /*border-right: 3px solid #D44147;*/
  border-right: 2px solid #d44147;
  box-sizing: border-box;
}

.paper-content {
  position: absolute;
  top: 30px;
  right: 0;
  bottom: 30px;
  left: 60px;
  background: linear-gradient(transparent, transparent 28px, #91d1d3 28px);
  background-size: 30px 30px;
}

.paper-content textarea {
  width: 100%;
  max-width: 100%;
  height: 100%;
  max-height: 100%;
  line-height: 30px;
  padding: 0 10px;
  border: 0;
  outline: 0;
  background: transparent;
  color: mediumblue;
  font-family: "Handlee", cursive;
  font-weight: bold;
  font-size: 18px;
  box-sizing: border-box;
  z-index: 1;
  resize: none;
  margin-top: 0px;
}

/* https://codepen.io/AlHakem/pen/BxXrKe */
.paper-shadow {
  position: relative;
  background: #fff;
  width: 665px;
  box-shadow: 0px 2px 38px rgba(0, 0, 0, 0.2);
  height: 400px;
  padding: 30px;
  overflow: scroll;
}
.paper-shadow:after,
.paper-shadow:before {
  content: "";
  position: absolute;
  left: auto;
  background: none;
  z-index: -1;
}
.paper-shadow:after {
  width: 90%;
  height: 10px;
  top: 30px;
  right: 8px;
  -webkit-transform: rotate(-3deg);
  -moz-transform: rotate(-3deg);
  -o-transform: rotate(-3deg);
  -ms-transform: rotate(-3deg);
  transform: rotate(-3deg);
  -webkit-box-shadow: 0px -20px 36px 5px #295d92;
  -moz-box-shadow: 0px -20px 36px 5px #295d92;
  box-shadow: 0px -25px 35px 0px rgba(0, 0, 0, 0.5);
}

.paper-shadow:before {
  width: 10px;
  height: 95%;
  top: 5px;
  right: 18px;
  -webkit-transform: rotate(3deg);
  -moz-transform: rotate(3deg);
  -o-transform: rotate(3deg);
  -ms-transform: rotate(3deg);
  transform: rotate(3deg);
  -webkit-box-shadow: 20px 0px 25px 5px #295d92;
  -moz-box-shadow: 20px 0px 25px 5px #295d92;
  box-shadow: 22px 0px 35px 0px rgba(0, 0, 0, 0.5);
}
</style>
