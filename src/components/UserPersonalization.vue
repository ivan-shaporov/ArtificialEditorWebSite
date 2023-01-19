<script setup lang="ts">
import { reactive, onMounted } from "vue";
import axios from "axios";

const props = defineProps(["modelValue", "clientPrincipal"])
const emit = defineEmits(['close'])

const styles = ["Friendly", "Busines", "Formal"]

const defaultPersonaliztion = {
  short: true,
  style: styles[0],
  language: "English",
}

const personalization = reactive(structuredClone(defaultPersonaliztion));

onMounted(GetUserPersonalization)

function GetUserPersonalization()
{
  axios.get("api/UserPersonalization")
    .then(response => {
      if (response.status == 200) {
        personalization.short = response.data.short;
        personalization.style = response.data.style;
        personalization.language = response.data.language;
      }
      else {
        console.log("cannot GetUserPersonalization "+ response.status)
      }
    })
    .catch((err) => {
      console.error("cannot GetUserPersonalization "+ err)
      Object.assign(personalization, defaultPersonaliztion);
    });
}

function save() {
  axios
    .post("api/UserPersonalization", personalization)
    .catch();
  emit('close');
}

function cancel() {
  GetUserPersonalization();
  emit('close');
}
</script>

<template>
    <div class="modal-backdrop">
      <div class="modal">
        <section class="modal-body">
          <h1>Personal preferences</h1>
          <table class="parameters">
            <tr>
              <td>Short:</td>
              <td><input type="checkbox" v-model="personalization.short" />&nbsp;keep it brief</td>
            </tr>
            <tr>
              <td>Style:</td>
              <td>
                <template v-for="s in styles" :key="s">
                  <input type="radio" :id="s" :value="s" v-model="personalization.style" /><label :for="s">{{s}}</label>
                </template>
              </td>
            </tr>
            <tr>
              <td>Result Language:</td>
              <td>
                <input v-model.trim="personalization.language"/>
                (experimental <i class="bi bi-info-circle warning" title="Not all languages work equaly well"></i>)
              </td>
            </tr>
          </table>
        </section>
        <footer class="modal-footer">
          <span v-if="!props.clientPrincipal">
            <a href=".auth/login/aad">Login</a> for personalization.
          </span>&nbsp;
          <button type="button" @click="save" :disabled="!props.clientPrincipal">Save</button>&nbsp;
          <button type="button" @click="cancel">Cancel</button>
        </footer>
      </div>
    </div>
</template>

<style scoped>
  .warning {
    color: red;
  }

  h1 {
    text-align: center;
    font-weight: bold;
    margin-top: 0px;
  }

  .parameters td:first-child {
    text-align: right;
    padding-right: 5px;
    font-weight: bold;
  }

  .modal-backdrop {
    position: fixed;
    top: 0;
    bottom: 0;
    left: 0;
    right: 0;
    background-color: rgba(0, 0, 0, 0.3);
    display: flex;
    justify-content: center;
    align-items: center;
    z-index: 1;
  }

  .modal {
    background: #FFFFFF;
    box-shadow: 2px 2px 20px 1px;
    overflow-x: auto;
    display: flex;
    flex-direction: column;
  }

  .modal-header,
  .modal-footer {
    padding: 15px;
    display: flex;
  }

  .modal-header {
    position: relative;
    border-bottom: 1px solid #eeeeee;
    color: #4AAE9B;
    justify-content: space-between;
  }

  .modal-footer {
    border-top: 1px solid #eeeeee;
    flex-direction: row;
    justify-content: flex-end;
  }

  .modal-body {
    position: relative;
    padding: 20px 10px;
  }

  .btn-close {
    position: absolute;
    top: 0;
    right: 0;
    border: none;
    font-size: 20px;
    padding: 10px;
    cursor: pointer;
    font-weight: bold;
    color: #4AAE9B;
    background: transparent;
  }

  .btn-green {
    color: white;
    background: #4AAE9B;
    border: 1px solid #4AAE9B;
    border-radius: 2px;
  }
</style>