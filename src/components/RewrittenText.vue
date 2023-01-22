<script setup lang="ts">

const props = defineProps(['text'])

async function copyRewritten() {
  await navigator.clipboard.writeText(props.text);
}
</script>

<template>
  <div class="container">
    <div id="rewritten" class="paper-shadow">{{ text }}>
    </div>
    <div id="toolbar">
      <span class="bi-clipboard" @click="copyRewritten" title="Copy"></span>
    </div>
  </div>
</template>

<style scoped>
.container {
  width: 665px;
}

#toolbar {
     line-height: 12px;
     width: 25px;
     font-size: 12pt;
     font-family: tahoma;
     margin-top: 4px;
     margin-right: 10px;
     position:absolute;
     top:0;
     right:0;
     cursor: pointer;
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
</style>