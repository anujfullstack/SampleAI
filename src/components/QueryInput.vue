<script setup lang="ts">
import { ref } from 'vue'

interface Props {
  query: string
  projectId: number
  loading: boolean
}

interface Emits {
  (e: 'update:query', value: string): void
  (e: 'update:projectId', value: number): void
  (e: 'submit'): void
  (e: 'clear'): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

const sampleQueries = [
  "List all participants interested in AI events",
  "Show me participants from the Marketing group",
  "Find all events with more than 5 interested participants",
  "What are the most popular interests?"
]

const useSampleQuery = (sample: string) => {
  emit('update:query', sample)
}
</script>

<template>
  <div class="card">
    <div class="mb-6">
      <label for="project-id" class="block text-sm font-medium text-slate-700 mb-2">
        Project ID
      </label>
      <input
        id="project-id"
        type="number"
        :value="projectId"
        @input="emit('update:projectId', parseInt(($event.target as HTMLInputElement).value))"
        class="w-32 p-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 outline-none"
        :disabled="loading"
      />
    </div>

    <div class="mb-6">
      <label for="query" class="block text-sm font-medium text-slate-700 mb-2">
        Your Question
      </label>
      <textarea
        id="query"
        :value="query"
        @input="emit('update:query', ($event.target as HTMLTextAreaElement).value)"
        placeholder="Ask a question about your data in plain English..."
        class="input-field h-32 resize-none"
        :disabled="loading"
      />
    </div>

    <div class="mb-6">
      <p class="text-sm font-medium text-slate-700 mb-3">Sample Queries:</p>
      <div class="flex flex-wrap gap-2">
        <button
          v-for="sample in sampleQueries"
          :key="sample"
          @click="useSampleQuery(sample)"
          class="btn-secondary text-sm"
          :disabled="loading"
        >
          {{ sample }}
        </button>
      </div>
    </div>

    <div class="flex gap-4">
      <button
        @click="emit('submit')"
        :disabled="loading || !query.trim()"
        class="btn-primary flex-1"
      >
        <span v-if="loading" class="flex items-center justify-center">
          <div class="loading-spinner mr-2"></div>
          Processing...
        </span>
        <span v-else>Ask Question</span>
      </button>
      
      <button
        @click="emit('clear')"
        :disabled="loading"
        class="btn-secondary"
      >
        Clear
      </button>
    </div>
  </div>
</template>