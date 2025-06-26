<script setup lang="ts">
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
</script>

<template>
  <div class="card">
    <div class="flex items-center justify-between mb-6">
      <div>
        <h3 class="text-xl font-semibold text-slate-800 flex items-center">
          <svg class="w-5 h-5 mr-2 text-blue-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-3.582 8-8 8a8.959 8.959 0 01-4.906-1.456L3 21l2.544-5.094A8.959 8.959 0 013 12c0-4.418 3.582-8 8-8s8 3.582 8 8z"/>
          </svg>
          Ask Your Question
        </h3>
        <p class="text-sm text-slate-600 mt-1">Type your question in natural language</p>
      </div>
      
      <!-- Project ID Input -->
      <div class="flex items-center space-x-2">
        <label for="project-id" class="text-sm font-medium text-slate-700">
          Project:
        </label>
        <input
          id="project-id"
          type="number"
          :value="projectId"
          @input="emit('update:projectId', parseInt(($event.target as HTMLInputElement).value))"
          class="w-20 px-3 py-2 text-sm border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
          :disabled="loading"
        />
      </div>
    </div>

    <!-- Query Input -->
    <div class="mb-6">
      <div class="relative">
        <textarea
          :value="query"
          @input="emit('update:query', ($event.target as HTMLTextAreaElement).value)"
          placeholder="Ask me anything about your data... 
For example: 'Show me all participants interested in AI' or 'What are the most popular events?'"
          class="input-field h-32 resize-none pr-12"
          :disabled="loading"
        />
        <div class="absolute bottom-3 right-3 flex items-center space-x-2">
          <div class="text-xs text-slate-400">
            {{ query.length }}/500
          </div>
          <svg class="w-5 h-5 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z"/>
          </svg>
        </div>
      </div>
    </div>

    <!-- Action Buttons -->
    <div class="flex gap-4">
      <button
        @click="emit('submit')"
        :disabled="loading || !query.trim()"
        class="btn-primary flex-1 flex items-center justify-center"
      >
        <span v-if="loading" class="flex items-center">
          <div class="loading-spinner mr-2"></div>
          Processing Query...
        </span>
        <span v-else class="flex items-center">
          <svg class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"/>
          </svg>
          Execute Query
        </span>
      </button>
      
      <button
        @click="emit('clear')"
        :disabled="loading"
        class="btn-secondary flex items-center"
      >
        <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/>
        </svg>
        Clear
      </button>
    </div>

    <!-- Query Status -->
    <div v-if="query.trim()" class="mt-4 p-3 bg-blue-50 border border-blue-200 rounded-lg">
      <div class="flex items-center text-sm text-blue-800">
        <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
        </svg>
        <span class="font-medium">Ready to execute:</span>
        <span class="ml-2 font-mono bg-white px-2 py-1 rounded text-xs">{{ query.slice(0, 50) }}{{ query.length > 50 ? '...' : '' }}</span>
      </div>
    </div>
  </div>
</template>