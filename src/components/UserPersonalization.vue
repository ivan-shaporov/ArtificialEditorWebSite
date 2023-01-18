<script setup lang="ts">
import { reactive, onMounted } from "vue";
import axios from "axios";

const props = defineProps(["modelValue", "clientPrincipal"])
const emit = defineEmits(['close'])

const styles = ["Friendly", "Busines", "Formal"]

const personalization = reactive({
  short: true,
  style: "Friendly",
  language: "English2",
});

onMounted(() => {
  axios.get("api/GetUserPersonalization")
    .then(response => {
      personalization.short = response.data.short;
      personalization.style = response.data.style;
      personalization.language = response.data.language;
    })
    .catch(() => console.log("cannot get personalization"));
})

function save() {
  axios
    .post("api/SaveUserPersonalization", personalization)
    .catch();
}

function close() {
    emit('close');
}
</script>

<template>
    <div class="modal-backdrop">
      <div class="modal">
        <section class="modal-body">
          <table>
            <tr>
              <td>Short</td>
              <td><input type="checkbox" v-model="personalization.short" /></td>
            </tr>
            <tr>
              <td>Style</td>
              <td>
                <template v-for="s in styles" :key="s">
                  <input type="radio" :id="s" :value="s" v-model="personalization.style" /><label :for="s">{{s}}</label>
                </template>
              </td>
            </tr>
            <tr>
              <td>Result Language<br/>(experimental, some languages might not work equaly well)</td>
              <td><input v-model.trim="personalization.language"/></td>
            </tr>
          </table>
        </section>
        <footer class="modal-footer">
          <span v-if="!props.clientPrincipal">
            <a href=".auth/login/aad">Login</a> for personalization.
          </span>&nbsp;
          <button type="button" @click="save" :disabled="props.clientPrincipal == ''">Save</button>&nbsp;
          <button type="button" @click="close">Cancel</button>
        </footer>
      </div>
    </div>
</template>

<style scoped>
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