<script setup lang="ts">
interface Props {
  data: any[]
  columns: string[]
  summary?: string | null
  insights?: any
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
        <div class="text-xs text-slate-500 bg-slate-100 px-2 py-1 rounded">
          {{ columns.length }} columns
        </div>
      </div>
    </div>

    <!-- Summary Section -->
    <div v-if="summary" class="mb-6 p-4 bg-blue-50 border border-blue-200 rounded-lg">
      <h4 class="text-sm font-semibold text-blue-800 mb-2">📊 Summary</h4>
      <p class="text-sm text-blue-700">{{ summary }}</p>
    </div>

    <!-- Insights Section -->
    <div v-if="insights" class="mb-6 p-4 bg-green-50 border border-green-200 rounded-lg">
      <h4 class="text-sm font-semibold text-green-800 mb-2">💡 Insights</h4>
      <div class="text-sm text-green-700">
        <pre class="whitespace-pre-wrap">{{ JSON.stringify(insights, null, 2) }}</pre>
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
            <th v-for="column in columns" :key="column">
              {{ column }}
            </th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(row, index) in data" :key="index" class="hover:bg-slate-50">
            <td v-for="column in columns" :key="column">
              {{ formatValue(row[column]) }}
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Data Statistics -->
    <div class="mt-6 grid grid-cols-1 md:grid-cols-3 gap-4">
      <div class="p-4 bg-blue-50 border border-blue-200 rounded-lg text-center">
        <p class="text-sm font-medium text-blue-800">Total Records</p>
        <p class="text-2xl font-bold text-blue-600">{{ data.length }}</p>
      </div>

      <div class="p-4 bg-green-50 border border-green-200 rounded-lg text-center">
        <p class="text-sm font-medium text-green-800">Columns</p>
        <p class="text-2xl font-bold text-green-600">{{ columns.length }}</p>
      </div>

      <div class="p-4 bg-purple-50 border border-purple-200 rounded-lg text-center">
        <p class="text-sm font-medium text-purple-800">Data Points</p>
        <p class="text-2xl font-bold text-purple-600">{{ data.length * columns.length }}</p>
      </div>
    </div>
  </div>
</template>