<script setup lang="ts">
interface Props {
  data: any[]
  tokenUsage?: {
    prompt: number
    completion: number
    total: number
  }
}

defineProps<Props>()

const formatValue = (value: any): string => {
  if (value === null || value === undefined) return 'N/A'
  if (typeof value === 'object') return JSON.stringify(value)
  return String(value)
}
</script>

<template>
  <div class="card">
    <div class="flex items-center justify-between mb-6">
      <h3 class="text-xl font-semibold text-slate-800">Query Results</h3>
      <div class="flex items-center gap-4">
        <span class="text-sm text-slate-600">{{ data.length }} results</span>
        <div v-if="tokenUsage" class="text-xs text-slate-500 bg-slate-100 px-2 py-1 rounded">
          Tokens: {{ tokenUsage.total }}
        </div>
      </div>
    </div>

    <div v-if="data.length === 0" class="text-center py-8 text-slate-500">
      <svg class="w-16 h-16 mx-auto mb-4 text-slate-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
      </svg>
      <p>No results found</p>
    </div>

    <div v-else class="table-container">
      <table class="data-table">
        <thead>
          <tr>
            <th v-for="column in Object.keys(data[0])" :key="column">
              {{ column }}
            </th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(row, index) in data" :key="index" class="hover:bg-slate-50">
            <td v-for="column in Object.keys(row)" :key="column">
              {{ formatValue(row[column]) }}
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-if="tokenUsage" class="mt-4 p-4 bg-slate-50 rounded-lg">
      <h4 class="text-sm font-medium text-slate-700 mb-2">Token Usage</h4>
      <div class="flex gap-4 text-sm text-slate-600">
        <span>Prompt: {{ tokenUsage.prompt }}</span>
        <span>Completion: {{ tokenUsage.completion }}</span>
        <span class="font-medium">Total: {{ tokenUsage.total }}</span>
      </div>
    </div>
  </div>
</template>