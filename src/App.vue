<script setup lang="ts">
import { reactive, ref } from "vue";
import axios from "axios";
import { ApplicationInsights } from "@microsoft/applicationinsights-web"

const appInsights = new ApplicationInsights({ config: {
  connectionString: "InstrumentationKey=4673dd3e-298b-4872-8900-83ac0a2d6bc4;IngestionEndpoint=https://westus2-2.in.applicationinsights.azure.com/;LiveEndpoint=https://westus2.livediagnostics.monitor.azure.com/"
  /* ...Other Configuration Options... */
} });
appInsights.loadAppInsights();
appInsights.trackPageView(); // Manually call trackPageView to establish the current user/session/pageview

var lastDraft: string;

const draft = ref("");

const defaultPlaceholder = `John Doe,

I lost your invoice.
Send it again.
If you don't send it I cannot pay.

Jack`;

const defaultRewrite = `Dear Mr. Doe,

I hope this email finds you well. I seem to have misplaced your invoice and I was wondering if you could send it to me again. As I am unable to locate it, I am unable to process payment at this time.

I apologize for any inconvenience this may have caused and appreciate your prompt attention to this matter.

Sincerely,
Jack`;

const rewritten = reactive({
  text: defaultRewrite,
  id: null,
  partition: null,
});

const rewriteEnabled = ref(true);
const reportEnabled = ref(false);
const allowLog = ref(false);

async function rewrite(): Promise<void> {
  if (draft.value.length == 0) {
    rewritten.text = defaultRewrite;
    return;
  }

  rewritten.text = "Let me think...";
  rewriteEnabled.value = false;
  const apiUrl = "api/RewriteEmail";
  let data = { text: draft.value, allowLog: allowLog.value };
  lastDraft = data.text;

  axios
    .post(apiUrl, data)
    .then((response) => {
      rewritten.text = response.data.text;
      rewritten.id = response.data.id;
      rewritten.partition = response.data.partition;
      reportEnabled.value = true;
    })
    .catch(() => {
      rewritten.text = "Failed to process, please retry later.";
    })
    .finally(() => {
      rewriteEnabled.value = true;
    });
}

async function reportProblem() {
  if (!rewritten.id) {
    return;
  }

  reportEnabled.value = false;
  const apiUrl = "api/ReportProblem";
  let data = {
    partition: rewritten.partition,
    id: rewritten.id,
    text: lastDraft,
    rewritten: rewritten.text,
  };

  axios
    .post(apiUrl, data)
    .catch(() => {
      rewritten.text = "Failed to report the problem, please retry later."; // todo: find a better place for the message.
    })
    .finally(() => {
      reportEnabled.value = true;
    });
}

async function copyRewritten() {
  await navigator.clipboard.writeText(rewritten.text);
}
</script>

<template>
  <main>
    <div>I am Artificial Intelligence that can rewrite e-mails for you. Given the text below:</div>
    <div class="paper">
      <div class="paper-content">
        <textarea autofocus maxlength="1000" rows="20" cols="100" id="draft" :placeholder="defaultPlaceholder" v-model="draft"/>
      </div>
    </div>

    <div>
      (allow reading by humans for improving the service <input id="allowLog" type="checkbox" v-model="allowLog" />)
      <input type="button" value="Rewrite" id="btnRewrite" @click="rewrite" :disabled="!rewriteEnabled" /> &nbsp;
      <input type="button" value="Report problem" id="btnReportProblem" @click="reportProblem" :disabled="!reportEnabled" />
    </div>
    
    <div>I would write it like this:</div>
    <div id="rewritten" class="paper-shadow">{{ rewritten.text }}
      <div id="btnCopy" @click="copyRewritten" title="Copy">📋</div>
    </div>
  </main>
</template>

<style scoped>
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
  overflow-y: auto;
  white-space: pre-wrap;
  color: black;
  font-family: Arial, Helvetica, sans-serif;
  font-size: 18px;
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

#btnCopy {
     line-height: 12px;
     width: 18px;
     font-size: 10pt;
     font-family: tahoma;
     margin-top: 4px;
     margin-right: 2px;
     position:absolute;
     top:0;
     right:0;
     cursor: pointer;
 }
</style>
