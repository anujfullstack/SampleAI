<script setup lang="ts">
import { ref, computed } from 'vue'

interface Emits {
  (e: 'select-query', query: string): void
}

const emit = defineEmits<Emits>()

const lastAskedQuery = ref('')
const searchTerm = ref('')

const sampleQueries = [
  "List Top 20 latest added participants who are not deleted and whose first or last name starts with the letter 'A', and who have both phone number and email having Application Id 1",
  "Show all participants from the 'Engineering' group who have Interest for both 'Cloud Computing' and 'DevOps' events",
  "List all participants who attended more than 2 events",
  "Which participants joined event where event date in between 1 june 2024 to 01 September 2024",
  "Which interest category has the highest number of registered participants?",
  "How many events were held per month in 2024, grouped by interest category?",
  "List the top 3 most attended events and how many participants joined them",
  "Which participants registered for events in the last 6 months of 2024",
  "List participants who registered for events more than 30 days after joining the company",
  "Who are the most active marketers interested in AI?",
  "Which participants from the Sales or Marketing group have attended an event in both June or September 2024?",
  "Give me the list of top 5 participants and count of events they've attended, sorted by most active",
  "Give me participant emails grouped by interest and Group category for the AI Conference 2024",
  "List all participants interested in AI events",
  "Show me participants from the Marketing group",
  "Find all events with more than 5 interested participants",
  "What are the most popular interests?"
]

const filteredQueries = computed(() => {
  if (!searchTerm.value.trim()) {
    return sampleQueries
  }
  return sampleQueries.filter(query => 
    query.toLowerCase().includes(searchTerm.value.toLowerCase())
  )
})

const selectQuery = (query: string) => {
  lastAskedQuery.value = query
  emit('select-query', query)
}

const clearLastAsked = () => {
  lastAskedQuery.value = ''
}
</script>

<template>
  <div class="sticky top-24 space-y-4">
    <!-- Last Asked Section -->
    <div v-if="lastAskedQuery" class="card">
      <div class="flex items-center justify-between mb-3">
        <h3 class="text-lg font-semibold text-slate-800 flex items-center">
          <svg class="w-4 h-4 mr-2 text-green-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"/>
          </svg>
          Last Asked
        </h3>
        <button 
          @click="clearLastAsked"
          class="text-slate-400 hover:text-slate-600 transition-colors"
          title="Clear last asked"
        >
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
          </svg>
        </button>
      </div>
      <div class="p-3 bg-green-50 border border-green-200 rounded-lg">
        <p class="text-sm text-green-800 font-medium mb-2">✅ Recently Executed:</p>
        <p class="text-xs text-green-700 font-mono bg-white/60 p-2 rounded border line-clamp-2">
          "{{ lastAskedQuery }}"
        </p>
        <button 
          @click="selectQuery(lastAskedQuery)"
          class="mt-2 text-xs text-green-600 hover:text-green-700 font-medium flex items-center"
        >
          <svg class="w-3 h-3 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/>
          </svg>
          Run Again
        </button>
      </div>
    </div>

    <!-- Sample Queries Section -->
    <div class="card">
      <div class="mb-4">
        <h3 class="text-lg font-semibold text-slate-800 mb-2 flex items-center">
          <svg class="w-4 h-4 mr-2 text-blue-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
          </svg>
          Sample Queries
        </h3>
        <p class="text-xs text-slate-600 mb-3">Click any query to try it instantly</p>
        
        <!-- Search Box -->
        <div class="relative">
          <input
            v-model="searchTerm"
            type="text"
            placeholder="Search queries..."
            class="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none pl-8"
          />
          <svg class="w-4 h-4 text-slate-400 absolute left-2.5 top-2.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/>
          </svg>
        </div>
      </div>

      <!-- Query List -->
      <div class="space-y-2 max-h-96 overflow-y-auto">
        <div
          v-for="(query, index) in filteredQueries"
          :key="index"
          @click="selectQuery(query)"
          class="group p-3 bg-slate-50 hover:bg-blue-50 rounded-lg border border-slate-200 hover:border-blue-300 cursor-pointer transition-all duration-200 hover:shadow-sm"
        >
          <div class="flex items-start justify-between">
            <div class="flex-1 min-w-0 pr-2">
              <p class="text-sm text-slate-700 group-hover:text-blue-700 transition-colors leading-relaxed line-clamp-2">
                {{ query }}
              </p>
            </div>
            <div class="flex-shrink-0">
              <svg class="w-4 h-4 text-slate-400 group-hover:text-blue-500 transition-colors" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/>
              </svg>
            </div>
          </div>
        </div>

        <!-- No Results -->
        <div v-if="filteredQueries.length === 0" class="text-center py-8 text-slate-500">
          <svg class="w-12 h-12 mx-auto mb-3 text-slate-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.172 16.172a4 4 0 015.656 0M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
          </svg>
          <p class="text-sm">No queries found</p>
          <p class="text-xs text-slate-400 mt-1">Try a different search term</p>
        </div>
      </div>

      <!-- Query Count -->
      <div class="mt-4 pt-3 border-t border-slate-200">
        <div class="flex items-center justify-between text-xs text-slate-500">
          <span>{{ filteredQueries.length }} of {{ sampleQueries.length }} queries</span>
          <div class="flex items-center space-x-1">
            <div class="w-2 h-2 bg-green-400 rounded-full"></div>
            <span>Ready to use</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Quick Tips -->
    <div class="card">
      <h4 class="text-sm font-semibold text-slate-800 mb-3 flex items-center">
        <span class="mr-2">💡</span>
        Quick Tips
      </h4>
      <ul class="space-y-2 text-xs text-slate-600">
        <li class="flex items-start">
          <span class="text-blue-500 mr-2">•</span>
          <span>Be specific with participant criteria</span>
        </li>
        <li class="flex items-start">
          <span class="text-blue-500 mr-2">•</span>
          <span>Use date ranges for time-based queries</span>
        </li>
        <li class="flex items-start">
          <span class="text-blue-500 mr-2">•</span>
          <span>Ask for counts, top results, or groupings</span>
        </li>
        <li class="flex items-start">
          <span class="text-blue-500 mr-2">•</span>
          <span>Combine multiple conditions for complex queries</span>
        </li>
      </ul>
    </div>
  </div>
</template>