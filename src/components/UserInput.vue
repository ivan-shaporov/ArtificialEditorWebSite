<script setup lang="ts">
import { computed, ref } from 'vue'
import UserPersonalization from "./UserPersonalization.vue";

const props = defineProps(["modelValue", "clientPrincipal"])
const emit = defineEmits(['update:modelValue'])

const value = computed({
  get() {
    return props.modelValue
  },
  set(value) {
    emit('update:modelValue', value)
  }
})

const placeholder = `John Doe,

I lost your invoice.
Send it again.
If you don't send it I cannot pay.

Jack`;

var isPersonalizationVisible = ref(false);

</script>

<template>
    <div class="paper">
        <div class="paper-content">
            <textarea autofocus maxlength="1000" rows="20" cols="100" id="draft" :placeholder="placeholder" v-model="value"/>
        </div>
        <div id="toolbar">
            <span class="bi-gear" @click="isPersonalizationVisible = true" title="Personalize"></span>&nbsp;
        </div>
    </div>
    <UserPersonalization v-show="isPersonalizationVisible" @close="isPersonalizationVisible = false" :clientPrincipal="props.clientPrincipal"/>
</template>

<style scoped>
#toolbar {
    line-height: 12px;
    width: 25px;
    font-size: 12pt;
    font-family: tahoma;
    margin-top: 0px;
    margin-right: 10px;
    position:absolute;
    top:0;
    right:0;
    cursor: pointer;
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
</style>
