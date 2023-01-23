<script setup lang="ts">
import { reactive, ref, onMounted } from "vue";
import axios from "axios";
import { ApplicationInsights } from "@microsoft/applicationinsights-web"
import UserInput from "./components/UserInput.vue";
import RewrittenText from "./components/RewrittenText.vue";

const appInsights = new ApplicationInsights({ config: {
  connectionString: "InstrumentationKey=4673dd3e-298b-4872-8900-83ac0a2d6bc4;IngestionEndpoint=https://westus2-2.in.applicationinsights.azure.com/;LiveEndpoint=https://westus2.livediagnostics.monitor.azure.com/"
  /* ...Other Configuration Options... */
} });
appInsights.loadAppInsights();
appInsights.trackPageView(); // Manually call trackPageView to establish the current user/session/pageview

const draft = ref("");
var lastDraft: string;

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

const clientPrincipal = ref("");

onMounted(() => {
  getClientPrincipal();
})

function getClientPrincipal() {
  console.log('requesting auth');
  axios.get(".auth/me")
    .then(response => {
      clientPrincipal.value = response.data.clientPrincipal;
      if (response.data.clientPrincipal) {
        var expiration = response.data.clientPrincipal.claims.filter((c: {typ: string}) => c.typ === "exp")[0]?.val;
        var interval = expiration - new Date().getTime() / 1000;
        console.log(`expiration: ${expiration}, interval:${interval}`);
        setTimeout(getClientPrincipal, interval);
      }
      else {
        console.log('auth not available');
      }
    })
    .catch(() => clientPrincipal.value = "");
}

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
</script>

<template>
  <main>
    <div>I am Artificial Intelligence that can rewrite e-mails for you. Given the text below:</div>
    <UserInput v-model="draft" :clientPrincipal="clientPrincipal"/>
    
    <div>
      (allow reading by humans for improving the service <input id="allowLog" type="checkbox" v-model="allowLog" />)
      <input type="button" value="Rewrite" id="btnRewrite" @click="rewrite" :disabled="!rewriteEnabled" /> &nbsp;
      <input type="button" value="Report problem" id="btnReportProblem" @click="reportProblem" :disabled="!reportEnabled" />
    </div>
    
    <div>I would write it like this:</div>    
    <RewrittenText :text="rewritten.text"/>
  </main>
</template>
